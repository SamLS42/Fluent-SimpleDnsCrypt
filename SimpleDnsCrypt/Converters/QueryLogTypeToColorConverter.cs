using SimpleDnsCrypt.Models;
using System.Globalization;
using System.Windows.Data;

namespace SimpleDnsCrypt.Converters;

public class QueryLogTypeToColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		QueryLogLineType logLineType = (QueryLogLineType)value;
		return logLineType switch
		{
			QueryLogLineType.A => "{DynamicResource AccentTextFillColorPrimaryBrush}",
			QueryLogLineType.Unknown => "{DynamicResource AccentTextFillColorPrimaryBrush}",
			QueryLogLineType.NS => "{DynamicResource AccentTextFillColorPrimaryBrush}",
			QueryLogLineType.CNAME => "{DynamicResource AccentTextFillColorPrimaryBrush}",
			QueryLogLineType.SOA => "{DynamicResource AccentTextFillColorPrimaryBrush}",
			QueryLogLineType.WKS => "{DynamicResource AccentTextFillColorPrimaryBrush}",
			QueryLogLineType.PTR => "#FF2a3b68", //normal
			QueryLogLineType.MX => "{DynamicResource AccentTextFillColorPrimaryBrush}",
			QueryLogLineType.TXT => "{DynamicResource AccentTextFillColorPrimaryBrush}",
			QueryLogLineType.AAAA => "#FFB32929", //Error?
			QueryLogLineType.SRV => "{DynamicResource AccentTextFillColorPrimaryBrush}",
			QueryLogLineType.ANY => "{DynamicResource AccentTextFillColorPrimaryBrush}",
			_ => "#FFB32929", //Error?
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
