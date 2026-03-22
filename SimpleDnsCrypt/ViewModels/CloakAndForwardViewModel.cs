using Caliburn.Micro;
using DnsCrypt.Blacklist;
using MahApps.Metro.Controls;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Win32;
using SimpleDnsCrypt.Config;
using SimpleDnsCrypt.Helper;
using SimpleDnsCrypt.Models;
using SimpleDnsCrypt.Windows;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using Application = System.Windows.Application;
using Screen = Caliburn.Micro.Screen;

namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(CloakAndForwardViewModel))]
public class CloakAndForwardViewModel : Screen
{
	private static readonly ILog Log = LogManagerHelper.Factory();
	private readonly IWindowManager _windowManager;
	private readonly IEventAggregator _events;
	private BindableCollection<Rule> _cloakingRules;
	private BindableCollection<Rule> _forwardingRules;

	private bool _isCloakingEnabled;
	private bool _isForwardingEnabled;
	private string _cloakingRulesFile;
	private string _forwardingRulesFile;

	/// <summary>
	/// Initializes a new instance of the <see cref="CloakAndForwardViewModel"/> class
	/// </summary>
	/// <param name="windowManager">The window manager</param>
	/// <param name="events">The events</param>
	[ImportingConstructor]
	public CloakAndForwardViewModel(IWindowManager windowManager, IEventAggregator events)
	{
		_windowManager = windowManager;
		_events = events;
		_events.Subscribe(this);
		_cloakingRules = [];
		_forwardingRules = [];

		if (!string.IsNullOrEmpty(Properties.Settings.Default.CloakingRulesFile))
		{
			_cloakingRulesFile = Properties.Settings.Default.CloakingRulesFile;
			Task.Run(async () => { await ReadCloakingRulesFromFile(); });
		}
		else
		{
			//set default
			_cloakingRulesFile = Path.Combine(Directory.GetCurrentDirectory(), Global.DnsCryptProxyFolder,
				Global.CloakingRulesFileName);
			Properties.Settings.Default.CloakingRulesFile = _cloakingRulesFile;
			Properties.Settings.Default.Save();
		}

		if (!string.IsNullOrEmpty(Properties.Settings.Default.ForwardingRulesFile))
		{
			_forwardingRulesFile = Properties.Settings.Default.ForwardingRulesFile;
			Task.Run(async () => { await ReadForwardingRulesFromFile(); });
		}
		else
		{
			//set default
			_forwardingRulesFile = Path.Combine(Directory.GetCurrentDirectory(), Global.DnsCryptProxyFolder,
				Global.ForwardingRulesFileName);
			Properties.Settings.Default.ForwardingRulesFile = _forwardingRulesFile;
			Properties.Settings.Default.Save();
		}
	}

	public bool IsWorking
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => IsWorking);
		}
	}

	#region Forwarding

	private async Task ReadForwardingRulesFromFile(string readFromPath = "")
	{
		try
		{
			string file = _forwardingRulesFile;
			if (!string.IsNullOrEmpty(readFromPath))
			{
				file = readFromPath;
			}

			if (string.IsNullOrEmpty(file))
				return;

			if (!File.Exists(file))
				return;
			string[] lines = await DomainBlacklist.ReadAllLinesAsync(file);
			if (lines.Length > 0)
			{
				List<Rule> tmpRules = [];
				foreach (string? line in lines)
				{
					if (line.StartsWith("#"))
						continue;
					string tmp = line.ToLower().Trim();
					if (string.IsNullOrEmpty(tmp))
						continue;
					string[] lineParts = tmp.Split([' '], StringSplitOptions.RemoveEmptyEntries);
					if (lineParts.Length != 2)
						continue;
					Rule rule = new()
					{
						Key = lineParts[0].Trim(),
						Value = lineParts[1].Trim()
					};

					tmpRules.Add(rule);
				}

				ForwardingRules.Clear();
				IOrderedEnumerable<Rule> orderedTmpRules = tmpRules.OrderBy(r => r.Key);
				ForwardingRules = new BindableCollection<Rule>(orderedTmpRules);
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public BindableCollection<Rule> ForwardingRules
	{
		get => _forwardingRules;
		set
		{
			if (value.Equals(_forwardingRules))
				return;
			_forwardingRules = value;
			NotifyOfPropertyChange(() => ForwardingRules);
		}
	}

	public Rule SelectedForwardingEntry
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => SelectedForwardingEntry);
		}
	}

	public string ForwardingRulesFile
	{
		get => _forwardingRulesFile;
		set
		{
			if (value.Equals(_forwardingRulesFile))
				return;
			_forwardingRulesFile = value;
			Properties.Settings.Default.ForwardingRulesFile = _forwardingRulesFile;
			Properties.Settings.Default.Save();
			NotifyOfPropertyChange(() => ForwardingRulesFile);
		}
	}

	public bool IsForwardingEnabled
	{
		get => _isForwardingEnabled;
		set
		{
			_isForwardingEnabled = value;
			ManageDnsCryptForwarding(DnscryptProxyConfigurationManager.DnscryptProxyConfiguration);
			NotifyOfPropertyChange(() => IsForwardingEnabled);
		}
	}

	private async void ManageDnsCryptForwarding(DnscryptProxyConfiguration dnscryptProxyConfiguration)
	{
		try
		{
			if (_isForwardingEnabled)
			{
				if (dnscryptProxyConfiguration == null)
					return;

				bool saveAndRestartService = false;

				if (dnscryptProxyConfiguration.forwarding_rules == null)
				{
					dnscryptProxyConfiguration.forwarding_rules = _forwardingRulesFile;
					saveAndRestartService = true;
				}

				if (!File.Exists(_forwardingRulesFile))
				{
					File.Create(_forwardingRulesFile).Dispose();
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
				//disable forwarding again
				_isForwardingEnabled = false;
				dnscryptProxyConfiguration.forwarding_rules = null;
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

	public void RemoveForwardingRule()
	{
		try
		{
			if (SelectedForwardingEntry == null)
				return;
			ForwardingRules.Remove(SelectedForwardingEntry);
			SaveForwardingRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public async void AddForwardingRule()
	{
		try
		{
			MetroWindow? metroWindow = Application.Current.Windows.OfType<MetroWindow>().FirstOrDefault();
			AddRuleWindow addRuleWindow = new(RuleWindowType.Forwarding);
			AddRuleWindowResult addRuleWindowResult = await metroWindow.ShowChildWindowAsync<AddRuleWindowResult>(addRuleWindow);

			if (!addRuleWindowResult.Result)
				return;
			if (string.IsNullOrEmpty(addRuleWindowResult.RuleKey) ||
				string.IsNullOrEmpty(addRuleWindowResult.RuleValue))
				return;
			Rule tmp = new()
			{
				Key = addRuleWindowResult.RuleKey,
				Value = addRuleWindowResult.RuleValue
			};
			_forwardingRules.Add(tmp);
			IOrderedEnumerable<Rule> orderedTmpRules = _forwardingRules.OrderBy(r => r.Key);
			ForwardingRules = new BindableCollection<Rule>(orderedTmpRules);
			SaveForwardingRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void ExportForwardingRules()
	{
		try
		{
			SaveFileDialog saveForwardingFileDialog = new()
			{
				RestoreDirectory = true,
				AddExtension = true,
				DefaultExt = ".txt"
			};
			bool? result = saveForwardingFileDialog.ShowDialog();
			if (result != true)
				return;
			SaveForwardingRulesToFile(saveForwardingFileDialog.FileName);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public async void ImportForwardingRules()
	{
		try
		{
			OpenFileDialog openForwardingFileDialog = new()
			{
				Multiselect = false,
				RestoreDirectory = true
			};
			bool? result = openForwardingFileDialog.ShowDialog();
			if (result != true)
				return;
			await ReadForwardingRulesFromFile(openForwardingFileDialog.FileName);
			SaveForwardingRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void ChangeForwardingRulesFile()
	{
		//try
		//{
		//	var forwardingFolderDialog = new FolderBrowserDialog
		//	{
		//		ShowNewFolderButton = true
		//	};
		//	if (!string.IsNullOrEmpty(_forwardingRulesFile))
		//	{
		//		forwardingFolderDialog.SelectedPath = Path.GetDirectoryName(_forwardingRulesFile);
		//	}

		//	var result = forwardingFolderDialog.ShowDialog();
		//	if (result == true)
		//	{
		//		ForwardingRulesFile = Path.Combine(forwardingFolderDialog.SelectedPath, Global.ForwardingRulesFileName);
		//		SaveForwardingRulesToFile();
		//	}
		//}
		//catch (Exception exception)
		//{
		//	Log.Error(exception);
		//}
	}

	private int LongestForwardingKey => this._forwardingRules.Max(z => z.Key.Length);

	public void SaveForwardingRulesToFile(string saveToPath = "")
	{
		try
		{
			string file = _forwardingRulesFile;
			if (!string.IsNullOrEmpty(saveToPath))
			{
				file = saveToPath;
			}

			if (string.IsNullOrEmpty(file))
				return;
			const int extraSpace = 1;
			List<string> lines = [];
			foreach (Rule rule in _forwardingRules)
			{
				int spaceCount = LongestForwardingKey - rule.Key.Length;
				StringBuilder sb = new();
				sb.Append(rule.Key);
				sb.Append(' ', spaceCount + extraSpace);
				sb.Append(rule.Value);
				lines.Add(sb.ToString());
			}

			IOrderedEnumerable<string> orderedTmpRules = lines.OrderBy(r => r);
			File.WriteAllLines(file, orderedTmpRules, Encoding.UTF8);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	#endregion

	#region Cloaking

	private async Task ReadCloakingRulesFromFile(string readFromPath = "")
	{
		try
		{
			string file = _cloakingRulesFile;
			if (!string.IsNullOrEmpty(readFromPath))
			{
				file = readFromPath;
			}

			if (string.IsNullOrEmpty(file))
				return;

			if (!File.Exists(file))
				return;
			string[] lines = await DomainBlacklist.ReadAllLinesAsync(file);
			if (lines.Length > 0)
			{
				List<Rule> tmpRules = [];
				foreach (string? line in lines)
				{
					if (line.StartsWith("#"))
						continue;
					string tmp = line.ToLower().Trim();
					if (string.IsNullOrEmpty(tmp))
						continue;
					string[] lineParts = tmp.Split([' '], StringSplitOptions.RemoveEmptyEntries);
					if (lineParts.Length != 2)
						continue;
					Rule rule = new()
					{
						Key = lineParts[0].Trim(),
						Value = lineParts[1].Trim()
					};

					tmpRules.Add(rule);
				}

				CloakingRules.Clear();
				IOrderedEnumerable<Rule> orderedTmpRules = tmpRules.OrderBy(r => r.Key);
				CloakingRules = new BindableCollection<Rule>(orderedTmpRules);
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	private int LongestCloakingKey => this._cloakingRules.Max(z => z.Key.Length);

	public void SaveCloakingRulesToFile(string saveToPath = "")
	{
		try
		{
			string file = _cloakingRulesFile;
			if (!string.IsNullOrEmpty(saveToPath))
			{
				file = saveToPath;
			}

			if (string.IsNullOrEmpty(file))
				return;
			const int extraSpace = 1;
			List<string> lines = [];
			foreach (Rule rule in _cloakingRules)
			{
				int spaceCount = LongestCloakingKey - rule.Key.Length;
				StringBuilder sb = new();
				sb.Append(rule.Key);
				sb.Append(' ', spaceCount + extraSpace);
				sb.Append(rule.Value);
				lines.Add(sb.ToString());
			}

			IOrderedEnumerable<string> orderedTmpRules = lines.OrderBy(r => r);
			File.WriteAllLines(file, orderedTmpRules, Encoding.UTF8);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public BindableCollection<Rule> CloakingRules
	{
		get => _cloakingRules;
		set
		{
			if (value.Equals(_cloakingRules))
				return;
			_cloakingRules = value;
			NotifyOfPropertyChange(() => CloakingRules);
		}
	}

	public string CloakingRulesFile
	{
		get => _cloakingRulesFile;
		set
		{
			if (value.Equals(_cloakingRulesFile))
				return;
			_cloakingRulesFile = value;
			Properties.Settings.Default.CloakingRulesFile = _cloakingRulesFile;
			Properties.Settings.Default.Save();
			NotifyOfPropertyChange(() => CloakingRulesFile);
		}
	}

	public bool IsCloakingEnabled
	{
		get => _isCloakingEnabled;
		set
		{
			_isCloakingEnabled = value;
			ManageDnsCryptCloaking(DnscryptProxyConfigurationManager.DnscryptProxyConfiguration);
			NotifyOfPropertyChange(() => IsCloakingEnabled);
		}
	}

	public void ChangeCloakingRulesFile()
	{
		//try
		//{
		//	var cloakingFolderDialog = new FolderBrowserDialog
		//	{
		//		ShowNewFolderButton = true
		//	};
		//	if (!string.IsNullOrEmpty(_cloakingRulesFile))
		//	{
		//		cloakingFolderDialog.SelectedPath = Path.GetDirectoryName(_cloakingRulesFile);
		//	}

		//	var result = cloakingFolderDialog.ShowDialog();
		//	if (result == true)
		//	{
		//		CloakingRulesFile = Path.Combine(cloakingFolderDialog.SelectedPath, Global.CloakingRulesFileName);
		//		SaveCloakingRulesToFile();
		//	}
		//}
		//catch (Exception exception)
		//{
		//	Log.Error(exception);
		//}
	}

	public Rule SelectedCloakingEntry
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => SelectedCloakingEntry);
		}
	}

	public void RemoveCloakingRule()
	{
		try
		{
			if (SelectedCloakingEntry == null)
				return;
			CloakingRules.Remove(SelectedCloakingEntry);
			SaveCloakingRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public async void AddCloakingRule()
	{
		try
		{
			MetroWindow? metroWindow = Application.Current.Windows.OfType<MetroWindow>().FirstOrDefault();
			AddRuleWindow addRuleWindow = new(RuleWindowType.Cloaking);
			AddRuleWindowResult addRuleWindowResult = await metroWindow.ShowChildWindowAsync<AddRuleWindowResult>(addRuleWindow);

			if (!addRuleWindowResult.Result)
				return;
			if (string.IsNullOrEmpty(addRuleWindowResult.RuleKey) ||
				string.IsNullOrEmpty(addRuleWindowResult.RuleValue))
				return;
			Rule tmp = new()
			{
				Key = addRuleWindowResult.RuleKey,
				Value = addRuleWindowResult.RuleValue
			};
			_cloakingRules.Add(tmp);
			IOrderedEnumerable<Rule> orderedTmpRules = _cloakingRules.OrderBy(r => r.Key);
			CloakingRules = new BindableCollection<Rule>(orderedTmpRules);
			SaveCloakingRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void ExportCloakingRules()
	{
		try
		{
			SaveFileDialog saveCloakingFileDialog = new()
			{
				RestoreDirectory = true,
				AddExtension = true,
				DefaultExt = ".txt"
			};
			bool? result = saveCloakingFileDialog.ShowDialog();
			if (result != true)
				return;
			SaveCloakingRulesToFile(saveCloakingFileDialog.FileName);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public async void ImportCloakingRules()
	{
		try
		{
			OpenFileDialog openCloakingFileDialog = new()
			{
				Multiselect = false,
				RestoreDirectory = true
			};
			bool? result = openCloakingFileDialog.ShowDialog();
			if (result != true)
				return;
			await ReadCloakingRulesFromFile(openCloakingFileDialog.FileName);
			SaveCloakingRulesToFile();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}


	private async void ManageDnsCryptCloaking(DnscryptProxyConfiguration dnscryptProxyConfiguration)
	{
		try
		{
			if (_isCloakingEnabled)
			{
				if (dnscryptProxyConfiguration == null)
					return;

				bool saveAndRestartService = false;

				if (dnscryptProxyConfiguration.cloaking_rules == null)
				{
					dnscryptProxyConfiguration.cloaking_rules = _cloakingRulesFile;
					saveAndRestartService = true;
				}

				if (!File.Exists(_cloakingRulesFile))
				{
					File.Create(_cloakingRulesFile).Dispose();
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
				//disable cloaking again
				_isCloakingEnabled = false;
				dnscryptProxyConfiguration.cloaking_rules = null;
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

	#endregion
}