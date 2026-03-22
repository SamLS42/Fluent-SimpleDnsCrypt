using SimpleDnsCrypt.Config;
using SimpleDnsCrypt.Helper;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Screen = Caliburn.Micro.Screen;

namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(ListenAddressesViewModel))]
[method: ImportingConstructor]
public class ListenAddressesViewModel() : Screen
{
	private ObservableCollection<string> _listenAddresses = [];

	/// <summary>
	///     The title of the window.
	/// </summary>
	public string WindowTitle
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => WindowTitle);
		}
	}

	public ObservableCollection<string> ListenAddresses
	{
		get => _listenAddresses;
		set
		{
			_listenAddresses = value;
			NotifyOfPropertyChange(() => ListenAddresses);
		}
	}

	public string SelectedListenAddress
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => SelectedListenAddress);
		}
	}

	public string AddressInput
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => AddressInput);
		}
	}

	public void AddAddress()
	{
		if (string.IsNullOrEmpty(AddressInput))
			return;
		string validatedAddress = ValidationHelper.ValidateIpEndpoint(AddressInput);
		if (string.IsNullOrEmpty(validatedAddress))
			return;
		if (ListenAddresses.Contains(validatedAddress))
			return;
		ListenAddresses.Add(validatedAddress);
		AddressInput = string.Empty;
	}

	public void RemoveAddress()
	{
		if (string.IsNullOrEmpty(SelectedListenAddress))
			return;
		if (_listenAddresses.Count == 1)
			return;
		_listenAddresses.Remove(SelectedListenAddress);
	}

	public void RestoreDefault()
	{
		ListenAddresses.Clear();
		ListenAddresses.Add(Global.DefaultResolverIpv4);
		ListenAddresses.Add(Global.DefaultResolverIpv6);
	}
}
