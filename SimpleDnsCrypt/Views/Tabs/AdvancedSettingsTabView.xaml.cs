using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace SimpleDnsCrypt.Views.Tabs;

public partial class AdvancedSettingsTabView : UserControl
{
	private static readonly Regex _nonNumericRegex = MyRegex();

	public AdvancedSettingsTabView()
	{
		InitializeComponent();
	}

	private void NetprobeTimeout_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		e.Handled=_nonNumericRegex.IsMatch(e.Text);
	}

	private void NetprobeTimeout_Pasting(object sender, DataObjectPastingEventArgs e)
	{
		if (e.DataObject.GetDataPresent(typeof(string)))
		{
			string text = (string)e.DataObject.GetData(typeof(string));
			if (_nonNumericRegex.IsMatch(text))
			{
				e.CancelCommand();
			}
		}
		else
		{
			e.CancelCommand();
		}
	}

	private void NetprobeTimeout_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key==Key.Space)
			e.Handled=true;
	}

	private void NetprobeTimeout_LostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is TextBox tb)
		{
			if (string.IsNullOrWhiteSpace(tb.Text))
			{
				tb.Text="0";
			}
			else if (!long.TryParse(tb.Text, out long value)||value<0)
			{
				tb.Text="0";
			}
			BindingExpression binding = tb.GetBindingExpression(TextBox.TextProperty);
			binding?.UpdateSource();
		}
	}

	[GeneratedRegex("[^0-9]+", RegexOptions.Compiled)]
	private static partial Regex MyRegex();
}
