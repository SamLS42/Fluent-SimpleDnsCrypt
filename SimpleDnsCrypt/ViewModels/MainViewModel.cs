using Caliburn.Micro;
using DnsCrypt.Models;
using SimpleDnsCrypt.Config;
using SimpleDnsCrypt.Extensions;
using SimpleDnsCrypt.Helper;
using SimpleDnsCrypt.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;
using Screen = Caliburn.Micro.Screen;
using TabControl = System.Windows.Controls.TabControl;

namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(MainViewModel))]
public partial class MainViewModel : Screen, INotifyDataErrorInfo
{
	private static readonly ILog Log = LogManagerHelper.Factory();
	private readonly IEventAggregator _events;
	private readonly IWindowManager _windowManager;
	private AddressBlacklistViewModel _addressBlacklistViewModel;
	private AddressBlockLogViewModel _addressBlockLogViewModel;
	private DnscryptProxyConfiguration _dnscryptProxyConfiguration;
	private DomainBlacklistViewModel _domainBlacklistViewModel;
	private DomainBlockLogViewModel _domainBlockLogViewModel;
	private CloakAndForwardViewModel _cloakAndForwardViewModel;
	private SettingsViewModel _settingsViewModel;
	private QueryLogViewModel _queryLogViewModel;
	private ListenAddressesViewModel _listenAddressesViewModel;
	private FallbackResolversViewModel _fallbackResolversViewModel;
	private RouteViewModel _routeViewModel;
	private ProxiesViewModel _proxiesViewModel;
	private bool _isDnsCryptAutomaticModeEnabled;
	private bool _isOperatingAsGlobalResolver;
	private bool _isResolverRunning;
	private bool _isServiceInstalled;
	private bool _isSavingConfiguration;
	private bool _isWorkingOnService;
	private int _windowWidth;
	private int _windowHeight;
	private BindableCollection<AvailableResolver> _resolvers;
	private readonly BindableCollection<StampFileEntry> _relays;
	private string _windowTitle;

	/// <summary>
	///     Initializes a new instance of the <see cref="MainViewModel" /> class
	/// </summary>
	/// <param name="windowManager">The window manager</param>
	/// <param name="events">The events</param>
	[ImportingConstructor]
	public MainViewModel(IWindowManager windowManager, IEventAggregator events)
	{
		Instance = this;
		_windowManager = windowManager;
		_events = events;
		_events.Subscribe(this);
		_windowWidth = Properties.Settings.Default.WindowWidth;
		_windowHeight = Properties.Settings.Default.WindowHeight;
		_windowTitle =
			$"{Global.ApplicationName} {VersionHelper.PublishVersion} {VersionHelper.PublishBuild} [dnscrypt-proxy {DnsCryptProxyManager.GetVersion()}]";
		SelectedTab = Tabs.MainTab;
		_isSavingConfiguration = false;
		_isWorkingOnService = false;

		_settingsViewModel = new SettingsViewModel(_windowManager, _events)
		{
			WindowTitle = LocalizationEx.GetUiString("settings", Thread.CurrentThread.CurrentCulture)
		};

		_settingsViewModel.PropertyChanged += SettingsViewModelOnPropertyChanged;
		_listenAddressesViewModel = new ListenAddressesViewModel();
		_fallbackResolversViewModel = new FallbackResolversViewModel();
		_routeViewModel = new RouteViewModel();
		_proxiesViewModel = new ProxiesViewModel(_windowManager, _events);
		_queryLogViewModel = new QueryLogViewModel(_windowManager, _events);
		_domainBlockLogViewModel = new DomainBlockLogViewModel();
		_domainBlacklistViewModel = new DomainBlacklistViewModel(_windowManager, _events);
		_addressBlockLogViewModel = new AddressBlockLogViewModel(_windowManager, _events);
		_addressBlacklistViewModel = new AddressBlacklistViewModel(_windowManager, _events);
		_cloakAndForwardViewModel = new CloakAndForwardViewModel(_windowManager, _events);
		_resolvers = [];
		_relays = [];
	}

	public Tabs SelectedTab { get; set; }

	public static MainViewModel Instance { get; set; }

	public DomainBlacklistViewModel DomainBlacklistViewModel
	{
		get => _domainBlacklistViewModel;
		set
		{
			if (value.Equals(_domainBlacklistViewModel))
				return;
			_domainBlacklistViewModel = value;
			NotifyOfPropertyChange(() => DomainBlacklistViewModel);
		}
	}

	public AddressBlacklistViewModel AddressBlacklistViewModel
	{
		get => _addressBlacklistViewModel;
		set
		{
			if (value.Equals(_addressBlacklistViewModel))
				return;
			_addressBlacklistViewModel = value;
			NotifyOfPropertyChange(() => AddressBlacklistViewModel);
		}
	}

	public DomainBlockLogViewModel DomainBlockLogViewModel
	{
		get => _domainBlockLogViewModel;
		set
		{
			if (value.Equals(_domainBlockLogViewModel))
				return;
			_domainBlockLogViewModel = value;
			NotifyOfPropertyChange(() => DomainBlockLogViewModel);
		}
	}

	public AddressBlockLogViewModel AddressBlockLogViewModel
	{
		get => _addressBlockLogViewModel;
		set
		{
			if (value.Equals(_addressBlockLogViewModel))
				return;
			_addressBlockLogViewModel = value;
			NotifyOfPropertyChange(() => AddressBlockLogViewModel);
		}
	}

	public CloakAndForwardViewModel CloakAndForwardViewModel
	{
		get => _cloakAndForwardViewModel;
		set
		{
			if (value.Equals(_cloakAndForwardViewModel))
				return;
			_cloakAndForwardViewModel = value;
			NotifyOfPropertyChange(() => CloakAndForwardViewModel);
		}
	}

	public QueryLogViewModel QueryLogViewModel
	{
		get => _queryLogViewModel;
		set
		{
			if (value.Equals(_queryLogViewModel))
				return;
			_queryLogViewModel = value;
			NotifyOfPropertyChange(() => QueryLogViewModel);
		}
	}

	public SettingsViewModel SettingsViewModel
	{
		get => _settingsViewModel;
		set
		{
			if (value.Equals(_settingsViewModel))
				return;
			_settingsViewModel = value;
			NotifyOfPropertyChange(() => SettingsViewModel);
		}
	}

	public ProxiesViewModel ProxiesViewModel
	{
		get => _proxiesViewModel;
		set
		{
			if (value.Equals(_proxiesViewModel))
				return;
			_proxiesViewModel = value;
			NotifyOfPropertyChange(() => ProxiesViewModel);
		}
	}

	public FallbackResolversViewModel FallbackResolversViewModel
	{
		get => _fallbackResolversViewModel;
		set
		{
			if (value.Equals(_fallbackResolversViewModel))
				return;
			_fallbackResolversViewModel = value;
			NotifyOfPropertyChange(() => FallbackResolversViewModel);
		}
	}

	public ListenAddressesViewModel ListenAddressesViewModel
	{
		get => _listenAddressesViewModel;
		set
		{
			if (value.Equals(_listenAddressesViewModel))
				return;
			_listenAddressesViewModel = value;
			NotifyOfPropertyChange(() => ListenAddressesViewModel);
		}
	}

	public RouteViewModel RouteViewModel
	{
		get => _routeViewModel;
		set
		{
			if (value.Equals(_routeViewModel))
				return;
			_routeViewModel = value;
			NotifyOfPropertyChange(() => RouteViewModel);
		}
	}

	public int SelectedTabIndex
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => SelectedTabIndex);
		}
	}

	public DnscryptProxyConfiguration DnscryptProxyConfiguration
	{
		get => _dnscryptProxyConfiguration;
		set
		{
			if (value.Equals(_dnscryptProxyConfiguration))
				return;
			_dnscryptProxyConfiguration = value;
			NotifyOfPropertyChange(() => DnscryptProxyConfiguration);
		}
	}

	/// <summary>
	///     The currently selected language.
	/// </summary>
	public Language SelectedLanguage
	{
		get;
		set
		{
			if (value.Equals(field))
				return;
			field = value;
			Properties.Settings.Default.PreferredLanguage = field.ShortCode;
			Properties.Settings.Default.Save();
			LocalizationEx.SetCulture(field.ShortCode);
			NotifyOfPropertyChange(() => SelectedLanguage);
		}
	}

	/// <summary>
	///     List of all available languages.
	/// </summary>
	public ObservableCollection<Language> Languages
	{
		get;
		set
		{
			if (value.Equals(field))
				return;
			field = value;
			NotifyOfPropertyChange(() => Languages);
		}
	}

	/// <summary>
	///     The title of the window.
	/// </summary>
	public string WindowTitle
	{
		get => _windowTitle;
		set
		{
			_windowTitle = value;
			NotifyOfPropertyChange(() => WindowTitle);
		}
	}

	/// <summary>
	///		The width of the main window.
	/// </summary>
	public int WindowWidth
	{
		get => _windowWidth;
		set
		{
			_windowWidth = value;
			NotifyOfPropertyChange(() => WindowWidth);
		}
	}

	/// <summary>
	///		The height of the main window.
	/// </summary>
	public int WindowHeight
	{
		get => _windowHeight;
		set
		{
			_windowHeight = value;
			NotifyOfPropertyChange(() => WindowHeight);
		}
	}

	private void SettingsViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
	{
		if (propertyChangedEventArgs != null)
		{
			if (propertyChangedEventArgs.PropertyName.Equals("IsInitialized") ||
				propertyChangedEventArgs.PropertyName.Equals("IsActive"))
				return;

			switch (propertyChangedEventArgs.PropertyName)
			{
				case "IsAdvancedSettingsTabVisible":
					if (!SettingsViewModel.IsAdvancedSettingsTabVisible)
						if (SelectedTab == Tabs.AdvancedSettingsTab)
							SelectedTabIndex = 0;
					break;
				case "IsQueryLogTabVisible":
					if (QueryLogViewModel.IsQueryLogLogging)
						QueryLogViewModel.IsQueryLogLogging = false;

					if (!SettingsViewModel.IsQueryLogTabVisible)
						if (SelectedTab == Tabs.QueryLogTab)
							SelectedTabIndex = 0;
					break;
				case "IsDomainBlockLogLogging":
					if (DomainBlockLogViewModel.IsDomainBlockLogLogging)
						DomainBlockLogViewModel.IsDomainBlockLogLogging = false;

					if (!SettingsViewModel.IsDomainBlockLogTabVisible)
						if (SelectedTab == Tabs.DomainBlockLogTab)
							SelectedTabIndex = 0;
					break;
				case "IsAddressBlockLogLogging":
					if (AddressBlockLogViewModel.IsAddressBlockLogLogging)
						AddressBlockLogViewModel.IsAddressBlockLogLogging = false;

					if (!SettingsViewModel.IsAddressBlockLogTabVisible)
						if (SelectedTab == Tabs.AddressBlockLogTab)
							SelectedTabIndex = 0;
					break;
				case "IsDomainBlacklistTabVisible":
					if (!SettingsViewModel.IsDomainBlacklistTabVisible)
						if (SelectedTab == Tabs.DomainBlacklistTab)
							SelectedTabIndex = 0;
					break;
				case "IsAddressBlacklistTabVisible":
					if (!SettingsViewModel.IsAddressBlacklistTabVisible)
						if (SelectedTab == Tabs.AddressBlacklistTab)
							SelectedTabIndex = 0;
					break;
				case "IsCloakAndForwardTabVisible":
					if (!SettingsViewModel.IsCloakAndForwardTabVisible)
						if (SelectedTab == Tabs.CloakAndForwardTab)
							SelectedTabIndex = 0;
					break;
			}
		}
	}

	public void TabControl_SelectionChanged(SelectionChangedEventArgs selectionChangedEventArgs)
	{
		try
		{
			if (selectionChangedEventArgs.Source.GetType() != typeof(TabControl))
				return;
			if (selectionChangedEventArgs.AddedItems.Count != 1)
				return;
			TabItem? tabItem = (TabItem)selectionChangedEventArgs.AddedItems[0];
			if (string.IsNullOrEmpty((string)tabItem.Tag))
				return;

			switch ((string)tabItem.Tag)
			{
				case "mainTab":
					SelectedTab = Tabs.MainTab;
					IsServiceInstalled = DnsCryptProxyManager.IsDnsCryptProxyInstalled();
					_isResolverRunning = DnsCryptProxyManager.IsDnsCryptProxyRunning();
					NotifyOfPropertyChange(() => IsResolverRunning);
					break;
				case "resolverTab":
					SelectedTab = Tabs.ResolverTab;
					LoadResolvers();
					break;
				case "advancedSettingsTab":
					SelectedTab = Tabs.AdvancedSettingsTab;
					break;
				case "queryLogTab":
					SelectedTab = Tabs.QueryLogTab;
					break;
				case "cloakAndForwardTab":
					SelectedTab = Tabs.CloakAndForwardTab;
					break;
				case "domainBlockLogTab":
					SelectedTab = Tabs.DomainBlockLogTab;
					break;
				case "domainBlacklistTab":
					SelectedTab = Tabs.DomainBlacklistTab;
					break;
				case "addressBlockLogTab":
					SelectedTab = Tabs.AddressBlockLogTab;
					break;
				case "addressBlacklistTab":
					SelectedTab = Tabs.AddressBlacklistTab;
					break;
				default:
					SelectedTab = Tabs.MainTab;
					break;
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}

	public async void About()
	{
		AboutViewModel win = new()
		{
			WindowTitle = LocalizationEx.GetUiString("about", Thread.CurrentThread.CurrentCulture)
		};
		dynamic settings = new ExpandoObject();
		settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		await _windowManager.ShowDialogAsync(win, null, settings);
	}

	public async void Settings()
	{
		dynamic settings = new ExpandoObject();
		settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		dynamic result = await _windowManager.ShowDialogAsync(SettingsViewModel, null, settings);
		if (!result)
			Properties.Settings.Default.Save();
	}

	public async void Proxies()
	{
		dynamic settings = new ExpandoObject();
		settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		ProxiesViewModel.WindowTitle = LocalizationEx.GetUiString("proxy_manage_proxies", Thread.CurrentThread.CurrentCulture);
		ProxiesViewModel.HttpProxyInput = string.IsNullOrEmpty(DnscryptProxyConfiguration.http_proxy) ? "" : DnscryptProxyConfiguration.http_proxy;
		ProxiesViewModel.SocksProxyInput = string.IsNullOrEmpty(DnscryptProxyConfiguration.proxy) ? "" : DnscryptProxyConfiguration.proxy;
		dynamic result = await _windowManager.ShowDialogAsync(ProxiesViewModel, null, settings);
		if (result)
			return;
		bool saveAdvancedSettings = false;

		if (string.IsNullOrEmpty(ProxiesViewModel.HttpProxyInput))
		{
			if (!string.IsNullOrEmpty(DnscryptProxyConfiguration.http_proxy))
			{
				DnscryptProxyConfiguration.http_proxy = null;
				saveAdvancedSettings = true;
			}
		}
		else
		{
			DnscryptProxyConfiguration.http_proxy = ProxiesViewModel.HttpProxyInput;
			saveAdvancedSettings = true;
		}

		if (string.IsNullOrEmpty(ProxiesViewModel.SocksProxyInput))
		{
			if (!string.IsNullOrEmpty(DnscryptProxyConfiguration.proxy))
			{
				DnscryptProxyConfiguration.proxy = null;
				saveAdvancedSettings = true;
			}
		}
		else
		{
			DnscryptProxyConfiguration.proxy = ProxiesViewModel.SocksProxyInput;
			saveAdvancedSettings = true;
		}

		if (saveAdvancedSettings)
		{
			SaveAdvancedSettings();
		}
	}

	public async Task ManageFallbackResolvers()
	{
		dynamic settings = new ExpandoObject();
		settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		List<string> oldAddressed = [.. DnscryptProxyConfiguration.listen_addresses];
		FallbackResolversViewModel.FallbackResolvers = DnscryptProxyConfiguration.fallback_resolvers;
		FallbackResolversViewModel.WindowTitle = LocalizationEx.GetUiString("advanced_settings_fallback_resolvers", Thread.CurrentThread.CurrentCulture);
		dynamic result = await _windowManager.ShowDialogAsync(FallbackResolversViewModel, null, settings);
		if (!result)
		{
			if (FallbackResolversViewModel.FallbackResolvers.Count == 0)
				return;

			List<string> a = [.. FallbackResolversViewModel.FallbackResolvers.Except(oldAddressed)];
			List<string> b = [.. oldAddressed.Except(FallbackResolversViewModel.FallbackResolvers)];
			if (!a.Any() && !b.Any())
				return;
			DnscryptProxyConfiguration.fallback_resolvers = FallbackResolversViewModel.FallbackResolvers;
			SaveAdvancedSettings();
		}
	}

	public async void ListenAddresses()
	{
		dynamic settings = new ExpandoObject();
		settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		List<string> oldAddressed = [.. DnscryptProxyConfiguration.listen_addresses];
		ListenAddressesViewModel.ListenAddresses = DnscryptProxyConfiguration.listen_addresses;
		ListenAddressesViewModel.WindowTitle = LocalizationEx.GetUiString("address_settings_listen_addresses", Thread.CurrentThread.CurrentCulture);
		dynamic result = await _windowManager.ShowDialogAsync(ListenAddressesViewModel, null, settings);
		if (!result)
		{
			if (ListenAddressesViewModel.ListenAddresses.Count == 0)
				return;

			List<string> a = [.. ListenAddressesViewModel.ListenAddresses.Except(oldAddressed)];
			List<string> b = [.. oldAddressed.Except(ListenAddressesViewModel.ListenAddresses)];
			if (!a.Any() && !b.Any())
				return;
			DnscryptProxyConfiguration.listen_addresses = ListenAddressesViewModel.ListenAddresses;
			SaveAdvancedSettings();

			List<LocalNetworkInterface> localNetworkInterfaces = LocalNetworkInterfaceManager.GetLocalNetworkInterfaces([.. oldAddressed]);
			foreach (LocalNetworkInterface localNetworkInterface in localNetworkInterfaces)
			{
				localNetworkInterface.IsChangeable = false;
				if (!localNetworkInterface.UseDnsCrypt)
					continue;
				bool status = LocalNetworkInterfaceManager.SetNameservers(localNetworkInterface,
					LocalNetworkInterfaceManager.ConvertToDnsList(
						[.. DnscryptProxyConfigurationManager.DnscryptProxyConfiguration.listen_addresses]));
				localNetworkInterface.UseDnsCrypt = status;
				localNetworkInterface.IsChangeable = true;
			}
			await Task.Delay(1000).ConfigureAwait(false);
			ReloadLoadNetworkInterfaces();
		}
	}

	/// <summary>
	/// Minimize the main window.
	/// </summary>
	public void Minimize()
	{
		if (GetView() is UIElement view)
		{
			Window window = Window.GetWindow(view);
			window?.WindowState = WindowState.Minimized;
		}
	}

	/// <summary>
	/// Close the main window.
	/// </summary>
	public void CloseWindow()
	{
		TryCloseAsync();
	}

	#region Tray

	#endregion
}