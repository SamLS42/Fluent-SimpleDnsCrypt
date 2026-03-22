using Caliburn.Micro;
using System.ComponentModel.Composition;
using Screen = Caliburn.Micro.Screen;

namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(AddressBlacklistViewModel))]
public class AddressBlacklistViewModel : Screen
{

	private readonly IWindowManager _windowManager;
	private readonly IEventAggregator _events;

	private BindableCollection<string> _addressBlacklist;

	/// <summary>
	/// Initializes a new instance of the <see cref="AddressBlacklistViewModel"/> class
	/// </summary>
	/// <param name="windowManager">The window manager</param>
	/// <param name="events">The events</param>
	[ImportingConstructor]
	public AddressBlacklistViewModel(IWindowManager windowManager, IEventAggregator events)
	{
		_windowManager = windowManager;
		_events = events;
		_events.Subscribe(this);
		_addressBlacklist = [];
		LoadAddressBlacklist();
	}

	public string SelectedAddressBlacklistEntry
	{
		get;
		set
		{
			if (value.Equals(field))
				return;
			field = value;
			NotifyOfPropertyChange(() => SelectedAddressBlacklistEntry);
		}
	}

	public BindableCollection<string> AddressBlacklist
	{
		get => _addressBlacklist;
		set
		{
			if (value.Equals(_addressBlacklist))
				return;
			_addressBlacklist = value;
			NotifyOfPropertyChange(() => AddressBlacklist);
		}
	}

	private void LoadAddressBlacklist()
	{
		try
		{

		}
		catch (Exception)
		{
		}
	}
}
