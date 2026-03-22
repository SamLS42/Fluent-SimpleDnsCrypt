using Caliburn.Micro;
using SimpleDnsCrypt.Helper;
using System.Text.Json.Serialization;

namespace SimpleDnsCrypt.Models;

public enum RouteState
{
	Empty = 0,
	Invalid = 1,
	Valid = 2
}

public class AvailableResolver : PropertyChangedBase
{
	[JsonIgnore]
	public string ToolTip => $"Ports: {string.Join(",", Ports.ToArray())}";
	[JsonIgnore]
	public string DisplayName => $"{Name} ({Protocol})";

	[JsonIgnore]
	public bool IsInServerList
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => IsInServerList);
		}
	}

	[JsonIgnore]
	public RouteState RouteState
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => RouteState);
		}
	}

	[JsonIgnore]
	public string RouteStateText => RouteState switch
	{
		RouteState.Empty => LocalizationEx.GetUiString("configure_routes_add", Thread.CurrentThread.CurrentCulture),
		RouteState.Invalid => LocalizationEx.GetUiString("configure_routes_invalid", Thread.CurrentThread.CurrentCulture),
		RouteState.Valid => LocalizationEx.GetUiString("configure_routes_change", Thread.CurrentThread.CurrentCulture),
		_ => LocalizationEx.GetUiString("configure_routes_unknown", Thread.CurrentThread.CurrentCulture),
	};

	[JsonIgnore]
	public Route Route
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Route);
		}
	}

	[JsonPropertyName("name")]
	public string Name
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Name);
		}
	}

	[JsonPropertyName("proto")]
	public string Protocol
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Protocol);
		}
	}

	[JsonPropertyName("ports")]
	public List<int> Ports
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Ports);
		}
	}

	[JsonPropertyName("ipv6")]
	public bool Ipv6
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Ipv6);
		}
	}

	[JsonPropertyName("dnssec")]
	public bool DnsSec
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => DnsSec);
		}
	}

	[JsonPropertyName("nolog")]
	public bool NoLog
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => NoLog);
		}
	}

	[JsonPropertyName("nofilter")]
	public bool NoFilter
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => NoFilter);
		}
	}

	[JsonPropertyName("description")]
	public string Description
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Description);
		}
	}
}
