using SimpleDnsCrypt.Models;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Windows.Data;

namespace SimpleDnsCrypt.Converters;

/// <summary>
///     LocalNetworkInterface to color converter.
/// </summary>
public class InUseToBackgroundConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		LocalNetworkInterface? localNetworkInterface = (LocalNetworkInterface)value;

		if (localNetworkInterface != null && localNetworkInterface.OperationalStatus != OperationalStatus.Up)
		{
			// red
			return "Red";
		}

		if (localNetworkInterface != null && localNetworkInterface.UseDnsCrypt)
		{
			// green
			return "Green";
		}

		// gray
		return "Gray";
	}

	public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
