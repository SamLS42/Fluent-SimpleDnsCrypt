using Caliburn.Micro;
using Application = System.Windows.Application;
using Screen = Caliburn.Micro.Screen;

namespace SimpleDnsCrypt.ViewModels;

public class SystemTrayViewModel(IWindowManager windowManager, IEventAggregator events, MainViewModel mainViewModel) : Screen
{
	private readonly IWindowManager _windowManager = windowManager;
	private readonly MainViewModel _mainViewModel = mainViewModel;
	private readonly IEventAggregator _events = events;

	protected override Task OnActivatedAsync(CancellationToken cancellationToken)
	{
		NotifyOfPropertyChange(() => CanShowWindow);
		NotifyOfPropertyChange(() => CanHideWindow);
		return base.OnActivatedAsync(cancellationToken);
	}

	public void ShowWindow()
	{
		if (!_mainViewModel.IsActive)
		{
			_windowManager.ShowWindowAsync(_mainViewModel);
		}
		NotifyOfPropertyChange(() => CanShowWindow);
		NotifyOfPropertyChange(() => CanHideWindow);
	}

	public bool CanShowWindow => !_mainViewModel.IsActive;

	public void HideWindow()
	{
		_mainViewModel.TryCloseAsync();

		NotifyOfPropertyChange(() => CanShowWindow);
		NotifyOfPropertyChange(() => CanHideWindow);
	}

	public bool CanHideWindow => _mainViewModel.IsActive;

	public void ExitApplication()
	{
		Application.Current.Shutdown();
	}
}
