using Caliburn.Micro;
using DnsCrypt.Blacklist;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using SimpleDnsCrypt.Config;
using SimpleDnsCrypt.Helper;
using SimpleDnsCrypt.Models;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using Application = System.Windows.Application;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Screen = Caliburn.Micro.Screen;


namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(DomainBlacklistViewModel))]
public class DomainBlacklistViewModel : Screen
{
	private static readonly ILog Log = LogManagerHelper.Factory();
	private readonly IWindowManager _windowManager;
	private readonly IEventAggregator _events;

	private BindableCollection<string> _domainBlacklistRules;
	private BindableCollection<string> _domainWhitelistRules;
	private string _domainWhitelistRuleFilePath;
	private string _domainBlacklistRuleFilePath;
	private bool _isBlacklistEnabled;
	private string _domainBlacklistFile;


	/// <summary>
	/// Initializes a new instance of the <see cref="DomainBlacklistViewModel"/> class
	/// </summary>
	/// <param name="windowManager">The window manager</param>
	/// <param name="events">The events</param>
	[ImportingConstructor]
	public DomainBlacklistViewModel(IWindowManager windowManager, IEventAggregator events)
	{
		_windowManager = windowManager;
		_events = events;
		_events.Subscribe(this);
		_domainBlacklistRules = [];
		_domainWhitelistRules = [];

		if (!string.IsNullOrEmpty(Properties.Settings.Default.DomainBlacklistFile))
		{
			_domainBlacklistFile = Properties.Settings.Default.DomainBlacklistFile;
		}
		else
		{
			//set default
			_domainBlacklistFile = Path.Combine(Directory.GetCurrentDirectory(), Global.DnsCryptProxyFolder, Global.BlacklistFileName);
			Properties.Settings.Default.DomainBlacklistFile = _domainBlacklistFile;
			Properties.Settings.Default.Save();
		}

		if (!string.IsNullOrEmpty(Properties.Settings.Default.DomainWhitelistRules))
		{
			_domainWhitelistRuleFilePath = Properties.Settings.Default.DomainWhitelistRules;
			Task.Run(async () =>
			{
				await ReadWhitelistRulesFromFile();
			});
		}
		else
		{
			//set default
			_domainWhitelistRuleFilePath = Path.Combine(Directory.GetCurrentDirectory(), Global.DnsCryptProxyFolder, Global.WhitelistRuleFileName);
			Properties.Settings.Default.DomainWhitelistRules = _domainWhitelistRuleFilePath;
			Properties.Settings.Default.Save();
		}

		if (!string.IsNullOrEmpty(Properties.Settings.Default.DomainBlacklistRules))
		{
			_domainBlacklistRuleFilePath = Properties.Settings.Default.DomainBlacklistRules;
			Task.Run(async () =>
			{
				await ReadBlacklistRulesFromFile();
			});
		}
		else
		{
			//set default
			_domainBlacklistRuleFilePath = Path.Combine(Directory.GetCurrentDirectory(), Global.DnsCryptProxyFolder, Global.BlacklistRuleFileName);
			Properties.Settings.Default.DomainBlacklistRules = _domainBlacklistRuleFilePath;
			Properties.Settings.Default.Save();
		}
	}

	public string DomainBlacklistFile
	{
		get => _domainBlacklistFile;
		set
		{
			if (value.Equals(_domainBlacklistFile))
				return;
			_domainBlacklistFile = value;
			Properties.Settings.Default.DomainBlacklistFile = _domainBlacklistFile;
			Properties.Settings.Default.Save();
			NotifyOfPropertyChange(() => DomainBlacklistFile);
		}
	}

	public bool IsBlacklistEnabled
	{
		get => _isBlacklistEnabled;
		set
		{
			_isBlacklistEnabled = value;
			ManageDnsCryptBlacklist(DnscryptProxyConfigurationManager.DnscryptProxyConfiguration);
			NotifyOfPropertyChange(() => IsBlacklistEnabled);
		}
	}

	private async void ManageDnsCryptBlacklist(DnscryptProxyConfiguration dnscryptProxyConfiguration)
	{
		const string defaultLogFormat = "ltsv";
		try
		{
			if (_isBlacklistEnabled)
			{
				if (dnscryptProxyConfiguration == null)
					return;

				bool saveAndRestartService = false;
				if (dnscryptProxyConfiguration.blocked_names == null)
				{
					dnscryptProxyConfiguration.blocked_names = new Blacklist
					{
						blocked_names_file = _domainBlacklistFile,
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

				if (string.IsNullOrEmpty(dnscryptProxyConfiguration.blocked_names.blocked_names_file))
				{
					dnscryptProxyConfiguration.blocked_names.blocked_names_file = _domainBlacklistFile;
					saveAndRestartService = true;
				}

				if (!File.Exists(_domainBlacklistFile))
				{
					File.Create(_domainBlacklistFile).Dispose();
					await Task.Delay(50);
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
			}
			else
			{
				//disable blacklist again
				_isBlacklistEnabled = false;
				dnscryptProxyConfiguration.blocked_names.blocked_names_file = null;
				DnscryptProxyConfigurationManager.DnscryptProxyConfiguration = dnscryptProxyConfiguration;
				DnscryptProxyConfigurationManager.SaveConfiguration();
				if (DnsCryptProxyManager.IsDnsCryptProxyRunning())
				{
					DnsCryptProxyManager.Restart();
					await Task.Delay(Global.ServiceRestartTime).ConfigureAwait(false);
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void ChangeBlacklistFilePath()
	{
		//try
		//{
		//	var blacklistFolderDialog = new FolderBrowserDialog
		//	{
		//		ShowNewFolderButton = true
		//	};
		//	if (!string.IsNullOrEmpty(_domainBlacklistFile))
		//	{
		//		blacklistFolderDialog.SelectedPath = Path.GetDirectoryName(_domainBlacklistFile);
		//	}
		//	var result = blacklistFolderDialog.ShowDialog();
		//	if (result == true)
		//	{
		//		DomainBlacklistFile = Path.Combine(blacklistFolderDialog.SelectedPath, Global.BlacklistFileName);
		//	}
		//}
		//catch (Exception exception)
		//{
		//	Log.Error(exception);
		//}
	}

	#region Whitelist

	public string SelectedDomainWhitelistEntry
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => SelectedDomainWhitelistEntry);
		}
	}

	public BindableCollection<string> DomainWhitelistRules
	{
		get => _domainWhitelistRules;
		set
		{
			if (value.Equals(_domainWhitelistRules))
				return;
			_domainWhitelistRules = value;
			NotifyOfPropertyChange(() => DomainWhitelistRules);
		}
	}

	public string DomainWhitelistRuleFilePath
	{
		get => _domainWhitelistRuleFilePath;
		set
		{
			if (value.Equals(_domainWhitelistRuleFilePath))
				return;
			_domainWhitelistRuleFilePath = value;
			Properties.Settings.Default.DomainWhitelistRules = _domainWhitelistRuleFilePath;
			Properties.Settings.Default.Save();
			SaveWhitelistRulesToFile();
			NotifyOfPropertyChange(() => DomainWhitelistRuleFilePath);
		}
	}

	public async void ImportWhitelistRules()
	{
		try
		{
			OpenFileDialog openWhitelistFileDialog = new()
			{
				Multiselect = false,
				RestoreDirectory = true
			};
			bool? result = openWhitelistFileDialog.ShowDialog();
			if (result == null)
				return;
			if (!result.Value)
				return;
			string[] whitelistLines = await DomainBlacklist.ReadAllLinesAsync(openWhitelistFileDialog.FileName);
			IEnumerable<string> parsed = DomainBlacklist.ParseBlacklist(whitelistLines, true);
			string[] enumerable = parsed as string[] ?? [.. parsed];
			if (enumerable.Length == 0)
				return;
			DomainWhitelistRules.Clear();
			DomainWhitelistRules = new BindableCollection<string>(enumerable);
			SaveWhitelistRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void ExportWhitelistRules()
	{
		try
		{
			SaveFileDialog saveWhitelistFileDialog = new()
			{
				RestoreDirectory = true,
				AddExtension = true,
				DefaultExt = ".txt"
			};
			bool? result = saveWhitelistFileDialog.ShowDialog();
			if (result != true)
				return;
			File.WriteAllLines(saveWhitelistFileDialog.FileName, _domainWhitelistRules);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void ChangeWhitelistRulesFilePath()
	{
		//try
		//{
		//	var whitelistFolderDialog = new FolderBrowserDialog
		//	{
		//		ShowNewFolderButton = true
		//	};
		//	if (!string.IsNullOrEmpty(_domainWhitelistRuleFilePath))
		//	{
		//		whitelistFolderDialog.SelectedPath = Path.GetDirectoryName(_domainWhitelistRuleFilePath);
		//	}
		//	var result = whitelistFolderDialog.ShowDialog();
		//	if (result == true)
		//	{
		//		DomainWhitelistRuleFilePath = Path.Combine(whitelistFolderDialog.SelectedPath, Global.WhitelistRuleFileName);
		//	}
		//}
		//catch (Exception exception)
		//{
		//	Log.Error(exception);
		//}
	}

	private async Task ReadWhitelistRulesFromFile()
	{
		try
		{
			if (string.IsNullOrEmpty(_domainWhitelistRuleFilePath))
				return;
			if (!File.Exists(_domainWhitelistRuleFilePath))
				return;
			string[] whitelist = await DomainBlacklist.ReadAllLinesAsync(_domainWhitelistRuleFilePath);
			DomainWhitelistRules.Clear();
			DomainWhitelistRules = new BindableCollection<string>(whitelist);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void SaveWhitelistRulesToFile()
	{
		try
		{
			if (!string.IsNullOrEmpty(_domainWhitelistRuleFilePath))
			{
				File.WriteAllLines(_domainWhitelistRuleFilePath, _domainWhitelistRules, Encoding.UTF8);
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void RemoveWhitelistRule()
	{
		try
		{
			if (string.IsNullOrEmpty(SelectedDomainWhitelistEntry))
				return;
			DomainWhitelistRules.Remove(SelectedDomainWhitelistEntry);
			SaveWhitelistRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public async void AddWhitelistRule()
	{
		try
		{
			MetroDialogSettings dialogSettings = new()
			{
				AffirmativeButtonText = LocalizationEx.GetUiString("add", Thread.CurrentThread.CurrentCulture),
				NegativeButtonText = LocalizationEx.GetUiString("cancel", Thread.CurrentThread.CurrentCulture),
				ColorScheme = MetroDialogColorScheme.Theme
			};

			MetroWindow? metroWindow = Application.Current.Windows.OfType<MetroWindow>().FirstOrDefault();
			string dialogResult = await metroWindow.ShowInputAsync(LocalizationEx.GetUiString("message_title_new_whitelist_rule", Thread.CurrentThread.CurrentCulture),
				LocalizationEx.GetUiString("message_content_new_whitelist_rule", Thread.CurrentThread.CurrentCulture), dialogSettings);

			if (string.IsNullOrEmpty(dialogResult))
				return;
			dialogResult = dialogResult.Replace(" ", "");
			string[] list = dialogResult.Split([','], StringSplitOptions.RemoveEmptyEntries);
			IEnumerable<string> parsed = DomainBlacklist.ParseBlacklist(list, true);
			string[] enumerable = parsed as string[] ?? [.. parsed];
			if (enumerable.Length <= 0)
				return;
			DomainWhitelistRules.AddRange(enumerable);
			SaveWhitelistRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}
	#endregion

	#region Blacklist
	public async Task BuildBlacklist()
	{
		string tmpFile = Path.GetTempFileName();
		try
		{
			MetroDialogSettings dialogSettings = new()
			{
				ColorScheme = MetroDialogColorScheme.Theme
			};
			MetroWindow? metroWindow = Application.Current.Windows.OfType<MetroWindow>().FirstOrDefault();

			Controls.BaseMetroDialog dialog = new();
			await metroWindow.ShowMetroDialogAsync(dialog, dialogSettings);

			BindableCollection<string> blacklistRules = _domainBlacklistRules;
			List<string> blacklistSource = [];
			List<string> blacklistLocalRules = [];
			foreach (string blacklistRule in blacklistRules)
			{
				if (blacklistRule.StartsWith("http://") ||
					blacklistRule.StartsWith("https://") ||
					blacklistRule.StartsWith("file:"))
				{
					blacklistSource.Add(blacklistRule);
				}
				else
				{
					blacklistLocalRules.Add(blacklistRule);
				}
			}

			File.WriteAllLines(tmpFile, blacklistLocalRules);
			blacklistSource.Add($"file:{tmpFile}");

			SortedSet<string> rules = await DomainBlacklist.Build(blacklistSource, [.. _domainWhitelistRules]);
			if (rules != null)
			{
				File.WriteAllLines(_domainBlacklistFile, rules);
			}

			if (DnsCryptProxyManager.IsDnsCryptProxyInstalled())
			{
				if (DnsCryptProxyManager.IsDnsCryptProxyRunning())
				{
					DnsCryptProxyManager.Restart();
					await Task.Delay(Global.ServiceRestartTime);
				}
			}

			await metroWindow.HideMetroDialogAsync(dialog);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
		finally
		{
			File.Delete(tmpFile);
		}
	}

	public void ClearDomainBlackList()
	{
		Execute.OnUIThread(() => { DomainBlacklistRules.Clear(); });
		SaveBlacklistRulesToFile();
		BuildBlacklist();
	}

	public async void AddBlacklistRule()
	{
		try
		{
			MetroDialogSettings dialogSettings = new()
			{
				AffirmativeButtonText = LocalizationEx.GetUiString("add", Thread.CurrentThread.CurrentCulture),
				NegativeButtonText = LocalizationEx.GetUiString("cancel", Thread.CurrentThread.CurrentCulture),
				ColorScheme = MetroDialogColorScheme.Theme
			};

			MetroWindow? metroWindow = Application.Current.Windows.OfType<MetroWindow>().FirstOrDefault();
			string dialogResult = await metroWindow.ShowInputAsync(LocalizationEx.GetUiString("message_title_new_blacklist_rule", Thread.CurrentThread.CurrentCulture),
				LocalizationEx.GetUiString("message_content_new_blacklist_rule", Thread.CurrentThread.CurrentCulture), dialogSettings);
			if (string.IsNullOrEmpty(dialogResult))
				return;
			dialogResult = dialogResult.Replace(" ", "");
			string[] list = dialogResult.Split([','], StringSplitOptions.RemoveEmptyEntries);

			List<string> remote = [];
			List<string> local = [];
			foreach (string l in list)
			{
				if (l.StartsWith("http://") || l.StartsWith("https://") || l.StartsWith("file:"))
				{
					remote.Add(l);
				}
				else
				{
					local.Add(l);
				}
			}
			IEnumerable<string> parsed = DomainBlacklist.ParseBlacklist(local, true);
			string[] enumerable = parsed as string[] ?? [.. parsed];
			if (enumerable.Length > 0)
			{
				DomainBlacklistRules.AddRange(enumerable);
			}

			DomainBlacklistRules.AddRange(remote);
			SaveBlacklistRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void RemoveBlacklistRule()
	{
		try
		{
			if (string.IsNullOrEmpty(SelectedDomainBlacklistEntry))
				return;
			DomainBlacklistRules.Remove(SelectedDomainBlacklistEntry);
			SaveBlacklistRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public async void ImportBlacklistRules()
	{
		try
		{
			OpenFileDialog openBlacklistFileDialog = new()
			{
				Multiselect = false,
				RestoreDirectory = true
			};
			bool? result = openBlacklistFileDialog.ShowDialog();
			if (result == null)
				return;
			if (!result.Value)
				return;
			string[] blacklistLines = await DomainBlacklist.ReadAllLinesAsync(openBlacklistFileDialog.FileName);
			IEnumerable<string> parsed = DomainBlacklist.ParseBlacklist(blacklistLines, true);
			string[] enumerable = parsed as string[] ?? [.. parsed];
			if (enumerable.Length == 0)
				return;
			DomainBlacklistRules.Clear();
			DomainBlacklistRules = new BindableCollection<string>(enumerable);
			SaveBlacklistRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void ExportBlacklistRules()
	{
		try
		{
			SaveFileDialog saveBlacklistFileDialog = new()
			{
				RestoreDirectory = true,
				AddExtension = true,
				DefaultExt = ".txt"
			};
			bool? result = saveBlacklistFileDialog.ShowDialog();
			if (result != true)
				return;
			File.WriteAllLines(saveBlacklistFileDialog.FileName, _domainBlacklistRules);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public string SelectedDomainBlacklistEntry
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => SelectedDomainBlacklistEntry);
		}
	}

	public BindableCollection<string> DomainBlacklistRules
	{
		get => _domainBlacklistRules;
		set
		{
			if (value.Equals(_domainBlacklistRules))
				return;
			_domainBlacklistRules = value;
			NotifyOfPropertyChange(() => DomainBlacklistRules);
		}
	}

	public string DomainBlacklistRuleFilePath
	{
		get => _domainBlacklistRuleFilePath;
		set
		{
			if (value.Equals(_domainBlacklistRuleFilePath))
				return;
			_domainBlacklistRuleFilePath = value;
			Properties.Settings.Default.DomainBlacklistFile = _domainBlacklistRuleFilePath;
			Properties.Settings.Default.Save();
			SaveBlacklistRulesToFile();
			NotifyOfPropertyChange(() => DomainBlacklistRuleFilePath);
		}
	}

	public void SaveBlacklistRulesToFile()
	{
		try
		{
			if (!string.IsNullOrEmpty(_domainBlacklistRuleFilePath))
			{
				File.WriteAllLines(_domainBlacklistRuleFilePath, _domainBlacklistRules, Encoding.UTF8);
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	private async Task ReadBlacklistRulesFromFile()
	{
		try
		{
			if (string.IsNullOrEmpty(_domainBlacklistRuleFilePath))
				return;
			if (!File.Exists(_domainBlacklistRuleFilePath))
				return;
			string[] blacklist = await DomainBlacklist.ReadAllLinesAsync(_domainBlacklistRuleFilePath);
			DomainBlacklistRules.Clear();
			DomainBlacklistRules = new BindableCollection<string>(blacklist);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void ChangeBlacklistRulesFilePath()
	{
		//.net 4.8 only
		//try
		//{
		//	var blacklistFolderDialog = new FolderBrowserDialog
		//	{
		//		ShowNewFolderButton = true
		//	};
		//	if (!string.IsNullOrEmpty(_domainBlacklistRuleFilePath))
		//	{
		//		blacklistFolderDialog.SelectedPath = Path.GetDirectoryName(_domainBlacklistRuleFilePath);
		//	}
		//	var result = blacklistFolderDialog.ShowDialog();
		//	if (result == true)
		//	{
		//		DomainBlacklistRuleFilePath = Path.Combine(blacklistFolderDialog.SelectedPath, Global.BlacklistRuleFileName);
		//	}
		//}
		//catch (Exception exception)
		//{
		//	Log.Error(exception);
		//}
	}
	#endregion
}
