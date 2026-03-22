using Caliburn.Micro;
using System.Net.NetworkInformation;

namespace SimpleDnsCrypt.Models;

public class LocalNetworkInterface : PropertyChangedBase
{
	private OperationalStatus _operationalStatus;
	private bool _isChangeable;

	public LocalNetworkInterface()
	{
		Dns = [];
		_isChangeable = true;
	}

	public string Name
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Name);
		}
	}

	/// <summary>
	/// The status of the network card (up/down)
	/// </summary>
	public OperationalStatus OperationalStatus
	{
		get => _operationalStatus;
		set
		{
			_operationalStatus = value;
			NotifyOfPropertyChange(() => OperationalStatus);
		}
	}

	public bool IsUp
	{
		get => _operationalStatus == OperationalStatus.Up;
		set
		{
			_operationalStatus = value ? OperationalStatus.Up : OperationalStatus.Down;
			NotifyOfPropertyChange(() => IsUp);
		}
	}

	public string Description
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Description);
		}
	}

	public NetworkInterfaceType Type
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Type);
		}
	}

	public List<DnsServer> Dns
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Dns);
		}
	}

	public bool Ipv6Support
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Ipv6Support);
		}
	}

	public bool Ipv4Support
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Ipv4Support);
		}
	}

	public bool IsChangeable
	{
		get => _isChangeable;
		set
		{
			_isChangeable = value;
			NotifyOfPropertyChange(() => IsChangeable);
		}
	}

	public bool UseDnsCrypt
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => UseDnsCrypt);
		}
	}
}
