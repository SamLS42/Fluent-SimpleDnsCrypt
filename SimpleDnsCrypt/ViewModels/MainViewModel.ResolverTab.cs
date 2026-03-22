using Caliburn.Micro;
using DnsCrypt.Models;
using SimpleDnsCrypt.Config;
using SimpleDnsCrypt.Extensions;
using SimpleDnsCrypt.Helper;
using SimpleDnsCrypt.Models;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Windows;

namespace SimpleDnsCrypt.ViewModels;

public partial class MainViewModel
{
	public bool IsDnsCryptAutomaticModeEnabled
	{
		get => _isDnsCryptAutomaticModeEnabled;
		set
		{
			if (value.Equals(_isDnsCryptAutomaticModeEnabled))
				return;
			_isDnsCryptAutomaticModeEnabled = value;
			if (_isDnsCryptAutomaticModeEnabled)
			{
				DnscryptProxyConfiguration.server_names = null;
				SaveDnsCryptConfiguration();
				LoadResolvers();
				HandleService();
			}
			else
			{
				if (DnscryptProxyConfiguration.server_names == null || DnscryptProxyConfiguration.server_names.Count == 0)
				{
					_isDnsCryptAutomaticModeEnabled = true;
					_windowManager.ShowMetroMessageBox(
						LocalizationEx.GetUiString("message_content_no_server_selected", Thread.CurrentThread.CurrentCulture),
						LocalizationEx.GetUiString("message_title_no_server_selected", Thread.CurrentThread.CurrentCulture),
						MessageBoxButton.OK, BoxType.Warning);
				}
			}

			NotifyOfPropertyChange(() => IsDnsCryptAutomaticModeEnabled);
		}
	}

	public BindableCollection<AvailableResolver> Resolvers
	{
		get => _resolvers;
		set
		{
			_resolvers = value;
			NotifyOfPropertyChange(() => Resolvers);
		}
	}

	public void SaveLocalServers()
	{
		if (DnscryptProxyConfiguration?.server_names?.Count > 0)
		{
			IsDnsCryptAutomaticModeEnabled = false;
			SaveDnsCryptConfiguration();
		}
		else
		{
			_windowManager.ShowMetroMessageBox(
				LocalizationEx.GetUiString("message_content_no_server_selected", Thread.CurrentThread.CurrentCulture),
				LocalizationEx.GetUiString("message_title_no_server_selected", Thread.CurrentThread.CurrentCulture),
				MessageBoxButton.OK, BoxType.Warning);
		}
	}

	public void ResolverClicked(AvailableResolver resolver)
	{
		if (resolver == null)
			return;
		if (resolver.IsInServerList)
		{
			if (DnscryptProxyConfiguration.server_names == null)
				return;
			if (DnscryptProxyConfiguration.server_names.Contains(resolver.Name))
				DnscryptProxyConfiguration.server_names.Remove(resolver.Name);
			resolver.IsInServerList = false;
		}
		else
		{
			if (DnscryptProxyConfiguration.server_names == null)
				DnscryptProxyConfiguration.server_names = [];
			if (!DnscryptProxyConfiguration.server_names.Contains(resolver.Name))
				DnscryptProxyConfiguration.server_names.Add(resolver.Name);
			resolver.IsInServerList = true;
		}
	}

	public async void HandleManageRoutes(AvailableResolver availableResolver)
	{
		try
		{
			if (availableResolver == null)
				return;
			if (!availableResolver.Protocol.Equals("DNSCrypt"))
				return;
			dynamic settings = new ExpandoObject();
			settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			RouteViewModel.Route = [];
			if (availableResolver.Route?.via != null)
			{
				for (int v = 0; v < availableResolver.Route.via.Count; v++)
				{
					StampFileEntry? stampFileEntry = _relays.FirstOrDefault(r => r.Name.Equals(availableResolver.Route.via[v]));
					if (stampFileEntry != null)
					{
						RouteViewModel.Route.Add(stampFileEntry);
					}
					else
					{
						availableResolver.Route.via.RemoveAt(v);
					}
				}
			}
			RouteViewModel.Relays = _relays;
			RouteViewModel.Resolver = availableResolver.DisplayName;
			dynamic result = await _windowManager.ShowDialogAsync(RouteViewModel, null, settings);
			if (result)
				return;

			if (!RouteViewModel.Route.Any())
			{
				if (availableResolver.Route == null)
					return;
				int oldRoute = _dnscryptProxyConfiguration.anonymized_dns.routes.FindIndex(r => r.server_name.Equals(availableResolver.Route.server_name));
				if (oldRoute != -1)
				{
					_dnscryptProxyConfiguration.anonymized_dns.routes.RemoveAt(oldRoute);
				}
				SaveDnsCryptConfiguration();
				LoadResolvers();
			}
			else
			{
				int oldRoute = -1;
				if (availableResolver.Route != null && !string.IsNullOrEmpty(availableResolver.Route.server_name))
				{
					oldRoute = _dnscryptProxyConfiguration.anonymized_dns.routes.FindIndex(r => r.server_name.Equals(availableResolver.Route.server_name));
				}
				if (oldRoute != -1)
				{
					_dnscryptProxyConfiguration.anonymized_dns.routes[oldRoute].via = [];
					foreach (StampFileEntry stampFileEntry in RouteViewModel.Route)
					{
						if (_dnscryptProxyConfiguration.anonymized_dns.routes[oldRoute].via == null)
						{
							_dnscryptProxyConfiguration.anonymized_dns.routes[oldRoute].via = [];
						}
						_dnscryptProxyConfiguration.anonymized_dns.routes[oldRoute].via.Add(stampFileEntry.Name);
					}
				}
				else
				{
					Route newRoute = new()
					{
						server_name = availableResolver.Name,
						via = []
					};
					foreach (StampFileEntry stampFileEntry in RouteViewModel.Route)
					{
						newRoute.via.Add(stampFileEntry.Name);
					}

					_dnscryptProxyConfiguration.anonymized_dns ??= new AnonymizedDns();
					if (_dnscryptProxyConfiguration.anonymized_dns.routes == null)
					{
						_dnscryptProxyConfiguration.anonymized_dns.routes = [];
					}
					_dnscryptProxyConfiguration.anonymized_dns.routes.Add(newRoute);
				}
				SaveDnsCryptConfiguration();
				LoadResolvers();
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
		finally
		{
			IsWorkingOnService = false;
		}
	}

	private void PrepareRoutes()
	{
		_relays.Clear();
		List<StampFileEntry> relays = RelayHelper.GetRelays();
		if (relays != null && relays.Count > 0)
		{
			_relays.AddRange(relays);
		}
	}

	private void LoadResolvers()
	{
		PrepareRoutes();
		List<AvailableResolver> availableResolvers = DnsCryptProxyManager.GetAvailableResolvers();
		List<AvailableResolver> allResolversWithoutFilters = DnsCryptProxyManager.GetAllResolversWithoutFilters();
		List<AvailableResolver> allResolversWithFilters = [];

		foreach (AvailableResolver resolver in allResolversWithoutFilters)
		{
			if (_dnscryptProxyConfiguration.doh_servers)
				if (!_dnscryptProxyConfiguration.dnscrypt_servers)
					if (!resolver.Protocol.Equals("DoH"))
						continue;

			if (_dnscryptProxyConfiguration.dnscrypt_servers)
				if (!_dnscryptProxyConfiguration.doh_servers)
					if (!resolver.Protocol.Equals("DNSCrypt"))
						continue;

			if (!_dnscryptProxyConfiguration.doh_servers && !_dnscryptProxyConfiguration.dnscrypt_servers)
				continue;

			if (_dnscryptProxyConfiguration.require_dnssec)
				if (!resolver.DnsSec)
					continue;

			if (_dnscryptProxyConfiguration.require_nofilter)
				if (!resolver.NoFilter)
					continue;

			if (_dnscryptProxyConfiguration.require_nolog)
				if (!resolver.NoLog)
					continue;

			if (resolver.Ipv6)
				if (!_dnscryptProxyConfiguration.ipv6_servers)
					continue;
			allResolversWithFilters.Add(resolver);
		}

		foreach (AvailableResolver resolver in availableResolvers)
		{
			AvailableResolver first = null;
			foreach (AvailableResolver r in allResolversWithFilters)
			{
				if (!r.Name.Equals(resolver.Name))
					continue;
				first = r;
				if (_dnscryptProxyConfiguration.anonymized_dns?.routes != null)
				{
					if (_dnscryptProxyConfiguration.anonymized_dns.routes.Count > 0)
					{
						Route? route = _dnscryptProxyConfiguration.anonymized_dns.routes.FirstOrDefault(re => re.server_name.Equals(resolver.Name));
						if (route != null)
						{
							first.Route = route;
							if (_relays != null && _relays.Count > 0)
							{
								List<string> relays = [.. _relays.Select(x => x.Name)];
								bool valid = first.Route.via.Intersect(relays).Count() == first.Route.via.Count();
								first.RouteState = valid ? RouteState.Valid : RouteState.Invalid;
							}
							else
							{
								first.RouteState = RouteState.Invalid;
							}
						}
					}
				}
				break;
			}

			first?.IsInServerList = true;
		}

		_resolvers.Clear();

		if (_isDnsCryptAutomaticModeEnabled)
		{
			foreach (AvailableResolver resolver in allResolversWithFilters)
			{
				resolver.IsInServerList = false;
			}
		}
		_resolvers.AddRange(allResolversWithFilters.OrderBy(o => o.DisplayName));
	}
}
