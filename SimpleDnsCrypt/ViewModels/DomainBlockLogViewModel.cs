using Caliburn.Micro;
using DnsCrypt.Blacklist;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using SimpleDnsCrypt.Config;
using SimpleDnsCrypt.Helper;
using SimpleDnsCrypt.Models;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using Application = System.Windows.Application;
using Screen = Caliburn.Micro.Screen;

namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(DomainBlockLogViewModel))]
[method: ImportingConstructor]
public class DomainBlockLogViewModel() : Screen
{
	private static readonly ILog Log = LogManagerHelper.Factory();

	private ObservableCollection<DomainBlockLogLine> _domainBlockLogLines = [];
	private bool _isDomainBlockLogLogging = false;

	private void AddLogLine(DomainBlockLogLine domainBlockLogLine)
	{
		Execute.OnUIThread(() =>
		{
			DomainBlockLogLines.Add(domainBlockLogLine);
		});
	}

	public void ClearDomainBlockLog()
	{
		Execute.OnUIThread(() => { DomainBlockLogLines.Clear(); });
	}

	public ObservableCollection<DomainBlockLogLine> DomainBlockLogLines
	{
		get => _domainBlockLogLines;
		set
		{
			if (value.Equals(_domainBlockLogLines))
				return;
			_domainBlockLogLines = value;
			NotifyOfPropertyChange(() => DomainBlockLogLines);
		}
	}

	public string DomainBlockLogFile
	{
		get;
		set
		{
			if (value.Equals(field))
				return;
			field = value;
			NotifyOfPropertyChange(() => DomainBlockLogFile);
		}
	}

	public DomainBlockLogLine SelectedDomainBlockLogLine
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => SelectedDomainBlockLogLine);
		}
	}

	public bool IsDomainBlockLogLogging
	{
		get => _isDomainBlockLogLogging;
		set
		{
			_isDomainBlockLogLogging = value;
			DomainBlockLog(DnscryptProxyConfigurationManager.DnscryptProxyConfiguration);
			NotifyOfPropertyChange(() => IsDomainBlockLogLogging);
		}
	}

	public async void UnblockBlockLogEntry()
	{
		try
		{
			if (SelectedDomainBlockLogLine == null)
				return;
			if (MainViewModel.Instance.DomainBlacklistViewModel == null)
				return;
			MetroDialogSettings dialogSettings = new()
			{
				DefaultText = SelectedDomainBlockLogLine.Message.ToLower(),
				AffirmativeButtonText = LocalizationEx.GetUiString("add", Thread.CurrentThread.CurrentCulture),
				NegativeButtonText = LocalizationEx.GetUiString("cancel", Thread.CurrentThread.CurrentCulture),
				ColorScheme = MetroDialogColorScheme.Theme
			};

			MetroWindow? metroWindow = Application.Current.Windows.OfType<MetroWindow>().FirstOrDefault();
			//TODO: translate
			string dialogResult = await metroWindow.ShowInputAsync(LocalizationEx.GetUiString("message_title_new_whitelist_rule", Thread.CurrentThread.CurrentCulture),
				"Rule:", dialogSettings);

			if (string.IsNullOrEmpty(dialogResult))
				return;
			string newCustomRule = dialogResult.ToLower().Trim();
			IEnumerable<string> parsed = DomainBlacklist.ParseBlacklist(newCustomRule, true);
			string[] enumerable = parsed as string[] ?? [.. parsed];
			if (enumerable.Length != 1)
				return;
			MainViewModel.Instance.DomainBlacklistViewModel.DomainWhitelistRules.Add(enumerable[0]);
			MainViewModel.Instance.DomainBlacklistViewModel.SaveWhitelistRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	private async void DomainBlockLog(DnscryptProxyConfiguration dnscryptProxyConfiguration)
	{
		const string defaultLogFormat = "ltsv";
		try
		{
			if (_isDomainBlockLogLogging)
			{
				if (dnscryptProxyConfiguration == null)
					return;

				bool saveAndRestartService = false;
				if (dnscryptProxyConfiguration.blocked_names == null)
				{
					dnscryptProxyConfiguration.blocked_names = new Blacklist
					{
						log_file = Global.DomainBlockLogFileName,
						log_format = defaultLogFormat
					};
					saveAndRestartService = true;
				}

				if (string.IsNullOrEmpty(dnscryptProxyConfiguration.blocked_names.log_format) ||
					!dnscryptProxyConfiguration.blocked_names.log_format.Equals(defaultLogFormat))
				{
					dnscryptProxyConfiguration.blocked_names.log_format = defaultLogFormat;
					saveAndRestartService = true;
				}

				if (string.IsNullOrEmpty(dnscryptProxyConfiguration.blocked_names.log_file))
				{
					dnscryptProxyConfiguration.blocked_names.log_file = Global.DomainBlockLogFileName;
					saveAndRestartService = true;
				}

				if (saveAndRestartService)
				{
					DnscryptProxyConfigurationManager.DnscryptProxyConfiguration = dnscryptProxyConfiguration;
					if (DnscryptProxyConfigurationManager.SaveConfiguration())
					{
						if (DnsCryptProxyManager.IsDnsCryptProxyInstalled())
						{
							if (DnsCryptProxyManager.IsDnsCryptProxyRunning())
							{
								DnsCryptProxyManager.Restart();
								await Task.Delay(Global.ServiceRestartTime).ConfigureAwait(false);
							}
							else
							{
								DnsCryptProxyManager.Start();
								await Task.Delay(Global.ServiceStartTime).ConfigureAwait(false);
							}
						}
						else
						{
							await Task.Run(() => DnsCryptProxyManager.Install()).ConfigureAwait(false);
							await Task.Delay(Global.ServiceInstallTime).ConfigureAwait(false);
							if (DnsCryptProxyManager.IsDnsCryptProxyInstalled())
							{
								DnsCryptProxyManager.Start();
								await Task.Delay(Global.ServiceStartTime).ConfigureAwait(false);
							}
						}
					}
				}

				DomainBlockLogFile = Path.Combine(Directory.GetCurrentDirectory(), Global.DnsCryptProxyFolder,
					dnscryptProxyConfiguration.blocked_names.log_file);

				if (!string.IsNullOrEmpty(DomainBlockLogFile))
				{
					if (!File.Exists(DomainBlockLogFile))
					{
						File.Create(DomainBlockLogFile).Dispose();
						await Task.Delay(50);
					}

					await Task.Run(() =>
					{
						using (StreamReader reader = new(new FileStream(DomainBlockLogFile,
							FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
						{
							//start at the end of the file
							long lastMaxOffset = reader.BaseStream.Length;

							while (_isDomainBlockLogLogging)
							{
								Thread.Sleep(500);
								//if the file size has not changed, idle
								if (reader.BaseStream.Length == lastMaxOffset)
									continue;

								//seek to the last max offset
								reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

								//read out of the file until the EOF
								string line;
								while ((line = reader.ReadLine()) != null)
								{
									DomainBlockLogLine blockLogLine = new(line);
									AddLogLine(blockLogLine);
								}

								//update the last max offset
								lastMaxOffset = reader.BaseStream.Position;
							}
						}
					}).ConfigureAwait(false);
				}
				else
				{
					IsDomainBlockLogLogging = false;
				}
			}
			else
			{
				//disable block log again
				_isDomainBlockLogLogging = false;
				dnscryptProxyConfiguration.blocked_names.log_file = null;
				DnscryptProxyConfigurationManager.DnscryptProxyConfiguration = dnscryptProxyConfiguration;
				DnscryptProxyConfigurationManager.SaveConfiguration();
				if (DnsCryptProxyManager.IsDnsCryptProxyRunning())
				{
					DnsCryptProxyManager.Restart();
					await Task.Delay(Global.ServiceRestartTime).ConfigureAwait(false);
				}
				Execute.OnUIThread(() => { DomainBlockLogLines.Clear(); });
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}
}
