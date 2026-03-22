using System.Globalization;
using System.Windows.Data;

namespace SimpleDnsCrypt.Converters;

public class BoolToForegroundConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((bool)value)
		{
			return "{DynamicResource AccentTextFillColorPrimaryBrush}";
		}

		return "Red";
	}

	public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
