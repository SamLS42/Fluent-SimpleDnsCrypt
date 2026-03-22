using Caliburn.Micro;
using System.ComponentModel.Composition;
using Screen = Caliburn.Micro.Screen;

namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(ListenAddressesViewModel))]
public class ProxiesViewModel : Screen
{
	private readonly IWindowManager _windowManager;
	private readonly IEventAggregator _events;

	public ProxiesViewModel()
	{
	}

	[ImportingConstructor]
	public ProxiesViewModel(IWindowManager windowManager, IEventAggregator events)
	{
		_windowManager = windowManager;
		_events = events;
		_events.Subscribe(this);
	}

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

	public string HttpProxyInput
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => HttpProxyInput);
		}
	}

	public string SocksProxyInput
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => SocksProxyInput);
		}
	}
}
