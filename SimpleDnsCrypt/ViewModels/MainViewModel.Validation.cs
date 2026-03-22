using SimpleDnsCrypt.Helper;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SimpleDnsCrypt.ViewModels;

public partial class MainViewModel
{
	public ObservableCollection<string> FallbackResolvers
	{
		get;
		set
		{
			field = value;
			ValidateFallbackResolvers();
			NotifyOfPropertyChange(() => FallbackResolvers);
		}
	}

	private readonly Dictionary<string, string> _validationErrors = [];

	public IEnumerable GetErrors(string propertyName)
	{
		return _validationErrors.TryGetValue(propertyName, out string? value) ? new List<string>(1) { value } : null;
	}

	public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

	public bool HasErrors => _validationErrors.Any();

	private void ValidateFallbackResolvers()
	{
		ObservableCollection<string> validatedFallbackResolvers = [];
		foreach (string fallbackResolver in FallbackResolvers)
		{
			if (string.IsNullOrEmpty(fallbackResolver))
			{
				_validationErrors.Add("FallbackResolvers", "invalid");
			}
			else
			{
				string validatedFallbackResolver = ValidationHelper.ValidateIpEndpoint(fallbackResolver);
				if (!string.IsNullOrEmpty(validatedFallbackResolver))
				{
					validatedFallbackResolvers.Add(validatedFallbackResolver);
					_validationErrors.Remove("FallbackResolvers");
				}
				else
				{
					_validationErrors.Add("FallbackResolvers", "invalid");
				}
			}
		}
		DnscryptProxyConfiguration.fallback_resolvers = validatedFallbackResolvers;
	}
}
