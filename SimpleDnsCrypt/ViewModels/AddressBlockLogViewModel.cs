using Caliburn.Micro;
using SimpleDnsCrypt.Helper;
using SimpleDnsCrypt.Models;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Screen = Caliburn.Micro.Screen;

namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(AddressBlockLogViewModel))]
public class AddressBlockLogViewModel : Screen
{
	private readonly IWindowManager _windowManager;
	private readonly IEventAggregator _events;

	private ObservableCollection<AddressBlockLogLine> _addressBlockLogLines;
	private bool _isAddressBlockLogLogging;

	[ImportingConstructor]
	public AddressBlockLogViewModel(IWindowManager windowManager, IEventAggregator events)
	{
		_windowManager = windowManager;
		_events = events;
		_events.Subscribe(this);
		_isAddressBlockLogLogging = false;
		_addressBlockLogLines = [];
	}

	private void AddLogLine(AddressBlockLogLine addressBlockLogLine)
	{
		Execute.OnUIThread(() =>
		{
			AddressBlockLogLines.Add(addressBlockLogLine);
		});
	}

	public void ClearAddressBlockLog()
	{
		Execute.OnUIThread(() => { AddressBlockLogLines.Clear(); });
	}

	public ObservableCollection<AddressBlockLogLine> AddressBlockLogLines
	{
		get => _addressBlockLogLines;
		set
		{
			if (value.Equals(_addressBlockLogLines))
				return;
			_addressBlockLogLines = value;
			NotifyOfPropertyChange(() => AddressBlockLogLines);
		}
	}

	public string AddressBlockLogFile
	{
		get;
		set
		{
			if (value.Equals(field))
				return;
			field = value;
			NotifyOfPropertyChange(() => AddressBlockLogFile);
		}
	}

	public AddressBlockLogLine SelectedAddressBlockLogLine
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => SelectedAddressBlockLogLine);
		}
	}

	public bool IsAddressBlockLogLogging
	{
		get => _isAddressBlockLogLogging;
		set
		{
			_isAddressBlockLogLogging = value;
			AddressBlockLog(DnscryptProxyConfigurationManager.DnscryptProxyConfiguration);
			NotifyOfPropertyChange(() => IsAddressBlockLogLogging);
		}
	}

	private void AddressBlockLog(DnscryptProxyConfiguration dnscryptProxyConfiguration)
	{

	}
}
