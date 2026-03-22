using SimpleDnsCrypt.Config;
using SimpleDnsCrypt.Helper;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Screen = Caliburn.Micro.Screen;

namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(FallbackResolversViewModel))]
[method: ImportingConstructor]
public class FallbackResolversViewModel() : Screen
{

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

	public ObservableCollection<string> FallbackResolvers
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => FallbackResolvers);
		}
	} = [];

	public string SelectedFallbackResolver
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => SelectedFallbackResolver);
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
		if (FallbackResolvers.Contains(validatedAddress))
			return;
		FallbackResolvers.Add(validatedAddress);
		AddressInput = string.Empty;
	}

	public void RemoveAddress()
	{
		if (string.IsNullOrEmpty(SelectedFallbackResolver))
			return;
		if (FallbackResolvers.Count == 1)
			return;
		FallbackResolvers.Remove(SelectedFallbackResolver);
	}

	public void RestoreDefault()
	{
		FallbackResolvers.Clear();
		FallbackResolvers = new ObservableCollection<string>(Global.DefaultFallbackResolvers);
	}
}
