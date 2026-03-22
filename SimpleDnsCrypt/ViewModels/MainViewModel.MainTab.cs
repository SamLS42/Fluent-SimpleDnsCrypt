using Caliburn.Micro;
using SimpleDnsCrypt.Config;
using SimpleDnsCrypt.Extensions;
using SimpleDnsCrypt.Helper;
using SimpleDnsCrypt.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace SimpleDnsCrypt.ViewModels;

public partial class MainViewModel
{
	public bool IsResolverRunning
	{
		get => _isResolverRunning;
		set
		{
			HandleService();
			NotifyOfPropertyChange(() => IsResolverRunning);
		}
	}

	public bool IsServiceInstalled
	{
		get => _isServiceInstalled;
		set
		{
			_isServiceInstalled = value;
			NotifyOfPropertyChange(() => IsServiceInstalled);
		}
	}

	public bool IsSavingConfiguration
	{
		get => _isSavingConfiguration;
		set
		{
			_isSavingConfiguration = value;
			NotifyOfPropertyChange(() => IsSavingConfiguration);
		}
	}

	public bool IsOperatingAsGlobalResolver
	{
		get => _isOperatingAsGlobalResolver;
		set
		{
			_isOperatingAsGlobalResolver = value;
			DnscryptProxyConfiguration.listen_addresses = _isOperatingAsGlobalResolver
				? [Global.GlobalResolver]
				: [Global.DefaultResolverIpv4, Global.DefaultResolverIpv6];
			NotifyOfPropertyChange(() => IsOperatingAsGlobalResolver);
		}
	}

	public bool IsWorkingOnService
	{
		get => _isWorkingOnService;
		set
		{
			_isWorkingOnService = value;
			NotifyOfPropertyChange(() => IsWorkingOnService);
		}
	}

	public BindableCollection<LocalNetworkInterface> LocalNetworkInterfaces
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => LocalNetworkInterfaces);
		}
	} = [];

	public bool IsUninstallingService
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => IsUninstallingService);
		}
	}

	public void OnClose(CancelEventArgs cancelEventArgs)
	{
		try
		{
			Properties.Settings.Default.WindowWidth = WindowWidth;
			Properties.Settings.Default.WindowHeight = WindowHeight;
			Properties.Settings.Default.Save();
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public bool ShowHiddenCards
	{
		get;
		set
		{
			field = value;
			ReloadLoadNetworkInterfaces();
			NotifyOfPropertyChange(() => ShowHiddenCards);
		}
	}

	public void Initialize()
	{
		if (DnsCryptProxyManager.IsDnsCryptProxyInstalled())
		{
			_isServiceInstalled = true;
			if (DnsCryptProxyManager.IsDnsCryptProxyRunning())
			{
				_isResolverRunning = true;
			}
		}
		else
		{
			_isServiceInstalled = false;
		}

		if (DnscryptProxyConfiguration != null && (DnscryptProxyConfiguration.server_names == null ||
												   DnscryptProxyConfiguration.server_names.Count == 0))
		{
			_isDnsCryptAutomaticModeEnabled = true;
		}
		else
		{
			_isDnsCryptAutomaticModeEnabled = false;
		}

		if (!string.IsNullOrEmpty(DnscryptProxyConfiguration?.query_log?.file))
		{
			QueryLogViewModel.IsQueryLogLogging = true;
		}

		if (!string.IsNullOrEmpty(DnscryptProxyConfiguration?.blocked_names?.log_file))
		{
			if (!File.Exists(DnscryptProxyConfiguration.blocked_names.log_file))
			{
				File.Create(DnscryptProxyConfiguration.blocked_names.log_file).Dispose();
			}
			DomainBlockLogViewModel.IsDomainBlockLogLogging = true;
		}

		if (!string.IsNullOrEmpty(DnscryptProxyConfiguration?.blocked_names?.blocked_names_file))
		{
			if (!File.Exists(DnscryptProxyConfiguration.blocked_names.blocked_names_file))
			{
				File.Create(DnscryptProxyConfiguration.blocked_names.blocked_names_file).Dispose();
			}
			DomainBlacklistViewModel.IsBlacklistEnabled = true;
		}

		if (DnscryptProxyConfiguration?.listen_addresses != null)
		{
			if (DnscryptProxyConfiguration.listen_addresses.Contains(Global.GlobalResolver))
			{
				_isOperatingAsGlobalResolver = true;
			}
		}

		if (DnscryptProxyConfiguration?.fallback_resolvers != null)
		{
			if (DnscryptProxyConfiguration.fallback_resolvers.Count > 0)
			{
				FallbackResolvers = DnscryptProxyConfiguration.fallback_resolvers;
			}
		}

		if (!string.IsNullOrEmpty(DnscryptProxyConfiguration?.cloaking_rules))
		{
			if (!File.Exists(DnscryptProxyConfiguration.cloaking_rules))
			{
				File.Create(DnscryptProxyConfiguration.cloaking_rules).Dispose();
			}
			CloakAndForwardViewModel.IsCloakingEnabled = true;
		}

		if (!string.IsNullOrEmpty(DnscryptProxyConfiguration?.forwarding_rules))
		{
			if (!File.Exists(DnscryptProxyConfiguration.forwarding_rules))
			{
				File.Create(DnscryptProxyConfiguration.forwarding_rules).Dispose();
			}
			CloakAndForwardViewModel.IsForwardingEnabled = true;
		}
	}

	public async void SaveDnsCryptConfiguration()
	{
		IsSavingConfiguration = true;
		try
		{
			if (DnscryptProxyConfiguration == null)
				return;
			DnscryptProxyConfigurationManager.DnscryptProxyConfiguration = _dnscryptProxyConfiguration;

			if (DnscryptProxyConfiguration?.server_names?.Count > 0)
			{
				IsDnsCryptAutomaticModeEnabled = false;
				ObservableCollection<string> selectedServerNames = DnscryptProxyConfiguration.server_names;
				foreach (string? serverName in selectedServerNames.ToList())
				{
					AvailableResolver? s = _resolvers.FirstOrDefault(r => r.Name.Equals(serverName));
					if (s == null)
					{
						selectedServerNames.Remove(serverName);
					}
					else
					{
						s.IsInServerList = true;
					}
				}

				DnscryptProxyConfiguration.server_names = selectedServerNames;
				if (DnscryptProxyConfiguration?.server_names?.Count == 0)
				{
					IsDnsCryptAutomaticModeEnabled = true;
				}
			}

			if (DnscryptProxyConfigurationManager.SaveConfiguration())
			{
				_dnscryptProxyConfiguration = DnscryptProxyConfigurationManager.DnscryptProxyConfiguration;
				IsWorkingOnService = true;
				if (DnsCryptProxyManager.IsDnsCryptProxyInstalled())
				{
					IsServiceInstalled = true;
					if (DnsCryptProxyManager.IsDnsCryptProxyRunning())
					{
						await Task.Run(() => { DnsCryptProxyManager.Restart(); }).ConfigureAwait(false);
						await Task.Delay(Global.ServiceRestartTime).ConfigureAwait(false);
					}
					else
					{
						await Task.Run(() => { DnsCryptProxyManager.Start(); }).ConfigureAwait(false);
						await Task.Delay(Global.ServiceStartTime).ConfigureAwait(false);
					}
				}
				else
				{
					IsServiceInstalled = false;
				}
			}

			_isResolverRunning = DnsCryptProxyManager.IsDnsCryptProxyRunning();
			NotifyOfPropertyChange(() => IsResolverRunning);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
		finally
		{
			IsSavingConfiguration = false;
			IsWorkingOnService = false;
		}
	}

	private async void HandleService()
	{
		IsWorkingOnService = true;
		if (IsResolverRunning)
		{
			await Task.Run(() => { DnsCryptProxyManager.Stop(); }).ConfigureAwait(false);
			await Task.Delay(Global.ServiceStopTime).ConfigureAwait(false);
			_isResolverRunning = DnsCryptProxyManager.IsDnsCryptProxyRunning();
			NotifyOfPropertyChange(() => IsResolverRunning);
		}
		else
		{
			if (DnsCryptProxyManager.IsDnsCryptProxyInstalled())
			{
				IsServiceInstalled = true;
				await Task.Run(() => { DnsCryptProxyManager.Start(); }).ConfigureAwait(false);
				await Task.Delay(Global.ServiceStartTime).ConfigureAwait(false);
				_isResolverRunning = DnsCryptProxyManager.IsDnsCryptProxyRunning();
				NotifyOfPropertyChange(() => IsResolverRunning);
			}
			else
			{
				await Task.Run(() => DnsCryptProxyManager.Install()).ConfigureAwait(false);
				await Task.Delay(Global.ServiceInstallTime).ConfigureAwait(false);
				if (DnsCryptProxyManager.IsDnsCryptProxyInstalled())
				{
					IsServiceInstalled = true;
					await Task.Run(() => { DnsCryptProxyManager.Start(); }).ConfigureAwait(false);
					await Task.Delay(Global.ServiceStartTime).ConfigureAwait(false);
				}
				else
				{
					IsServiceInstalled = false;
				}

				_isResolverRunning = DnsCryptProxyManager.IsDnsCryptProxyRunning();
				NotifyOfPropertyChange(() => IsResolverRunning);
			}
		}

		IsWorkingOnService = false;
	}

	private void ReloadLoadNetworkInterfaces()
	{
		List<LocalNetworkInterface> localNetworkInterfaces;
		if (_isOperatingAsGlobalResolver)
		{
			List<string> dnsServer =
			[
				Global.DefaultResolverIpv4,
				Global.DefaultResolverIpv6
			];
			localNetworkInterfaces = LocalNetworkInterfaceManager.GetLocalNetworkInterfaces(
				dnsServer, ShowHiddenCards);
		}
		else
		{
			localNetworkInterfaces = LocalNetworkInterfaceManager.GetLocalNetworkInterfaces(
				[.. DnscryptProxyConfigurationManager.DnscryptProxyConfiguration.listen_addresses], ShowHiddenCards);
		}

		LocalNetworkInterfaces.Clear();

		if (localNetworkInterfaces.Count == 0)
			return;

		foreach (LocalNetworkInterface localNetworkInterface in localNetworkInterfaces)
			LocalNetworkInterfaces.Add(localNetworkInterface);
	}

	public async void NetworkCardClicked(LocalNetworkInterface localNetworkInterface)
	{
		if (localNetworkInterface == null)
			return;
		if (!localNetworkInterface.IsChangeable)
			return;
		localNetworkInterface.IsChangeable = false;
		if (localNetworkInterface.UseDnsCrypt)
		{
			bool status = LocalNetworkInterfaceManager.UnsetNameservers(localNetworkInterface);
			localNetworkInterface.UseDnsCrypt = !status;
		}
		else
		{
			if (DnsCryptProxyManager.IsDnsCryptProxyRunning())
				if (_isOperatingAsGlobalResolver)
				{
					List<string> dnsServer =
					[
						Global.DefaultResolverIpv4,
						Global.DefaultResolverIpv6
					];
					bool status = LocalNetworkInterfaceManager.SetNameservers(localNetworkInterface,
						LocalNetworkInterfaceManager.ConvertToDnsList(dnsServer));
					localNetworkInterface.UseDnsCrypt = status;
				}
				else
				{
					bool status = LocalNetworkInterfaceManager.SetNameservers(localNetworkInterface,
						LocalNetworkInterfaceManager.ConvertToDnsList(
							[.. DnscryptProxyConfigurationManager.DnscryptProxyConfiguration.listen_addresses]));
					localNetworkInterface.UseDnsCrypt = status;
				}
			else
				_windowManager.ShowMetroMessageBox(
					LocalizationEx.GetUiString("message_content_service_not_running", Thread.CurrentThread.CurrentCulture),
					LocalizationEx.GetUiString("message_title_service_not_running", Thread.CurrentThread.CurrentCulture),
					MessageBoxButton.OK, BoxType.Warning);
		}

		await Task.Delay(1000).ConfigureAwait(false);
		localNetworkInterface.IsChangeable = true;
		ReloadLoadNetworkInterfaces();
	}

	public void OpenLogDirectory()
	{
		try
		{
			string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), Global.LogDirectory);
			if (!Directory.Exists(logDirectory))
			{
				Directory.CreateDirectory(logDirectory);
			}
			Process.Start(logDirectory);
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public void SaveAdvancedSettings()
	{
		if (!HasErrors)
		{
			SaveDnsCryptConfiguration();
		}
	}

	public async void UninstallService()
	{
		MessageBoxResult result = _windowManager.ShowMetroMessageBox(
			LocalizationEx.GetUiString("dialog_message_uninstall", Thread.CurrentThread.CurrentCulture),
			LocalizationEx.GetUiString("dialog_uninstall_title", Thread.CurrentThread.CurrentCulture),
			MessageBoxButton.YesNo, BoxType.Default);

		if (result != MessageBoxResult.Yes)
			return;
		IsUninstallingService = true;

		if (DnsCryptProxyManager.IsDnsCryptProxyRunning())
		{
			await Task.Run(() => { DnsCryptProxyManager.Stop(); }).ConfigureAwait(false);
			await Task.Delay(Global.ServiceStopTime).ConfigureAwait(false);
		}

		await Task.Run(() => { DnsCryptProxyManager.Uninstall(); }).ConfigureAwait(false);
		await Task.Delay(Global.ServiceUninstallTime).ConfigureAwait(false);
		_isResolverRunning = DnsCryptProxyManager.IsDnsCryptProxyRunning();
		NotifyOfPropertyChange(() => IsResolverRunning);

		List<LocalNetworkInterface> localNetworkInterfaces = LocalNetworkInterfaceManager.GetLocalNetworkInterfaces(
			[.. DnscryptProxyConfigurationManager.DnscryptProxyConfiguration.listen_addresses]);
		foreach (LocalNetworkInterface localNetworkInterface in localNetworkInterfaces)
		{
			if (!localNetworkInterface.UseDnsCrypt)
				continue;
			bool status = LocalNetworkInterfaceManager.SetNameservers(localNetworkInterface, []);
			LocalNetworkInterface? card = LocalNetworkInterfaces.SingleOrDefault(n => n.Description.Equals(localNetworkInterface.Description));
			card?.UseDnsCrypt = !status;
		}

		await Task.Delay(1000).ConfigureAwait(false);
		ReloadLoadNetworkInterfaces();
		IsUninstallingService = false;
		if (!DnsCryptProxyManager.IsDnsCryptProxyInstalled())
		{
			IsServiceInstalled = false;
			_windowManager.ShowMetroMessageBox(
				LocalizationEx.GetUiString("message_content_uninstallation_successful",
					Thread.CurrentThread.CurrentCulture),
				LocalizationEx.GetUiString("message_title_uninstallation_successful",
					Thread.CurrentThread.CurrentCulture),
				MessageBoxButton.OK, BoxType.Default);
		}
		else
		{
			IsServiceInstalled = true;
			_windowManager.ShowMetroMessageBox(
				LocalizationEx.GetUiString("message_content_uninstallation_error",
					Thread.CurrentThread.CurrentCulture),
				LocalizationEx.GetUiString("message_title_uninstallation_error",
					Thread.CurrentThread.CurrentCulture),
				MessageBoxButton.OK, BoxType.Warning);
		}
	}
}
