using SimpleDnsCrypt.Models;
using System.ComponentModel.Composition;
using System.Windows;
using Screen = Caliburn.Micro.Screen;

namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(MetroMessageBoxViewModel))]
public class MetroMessageBoxViewModel : Screen
{

	/// <summary>
	///     MetroMessageBoxViewModel constructor.
	/// </summary>
	/// <param name="message">The message to show.</param>
	/// <param name="title">The title of the box.</param>
	/// <param name="buttons">Buttons to show up.</param>
	/// <param name="messageBoxType">The type of the box.</param>
	public MetroMessageBoxViewModel(string message, string title, MessageBoxButton buttons,
		BoxType messageBoxType)
	{
		if (title != null)
			Title = title;
		if (message != null)
			Message = message;
		MessageBoxType = messageBoxType;
		Buttons = buttons;
	}

	/// <summary>
	///     The MessageBox title.
	/// </summary>
	public string Title
	{
		get;
		set
		{
			if (value.Equals(field))
				return;
			field = value;
			NotifyOfPropertyChange(() => Title);
		}
	}

	/// <summary>
	///     The MessageBox type.
	/// </summary>
	public BoxType MessageBoxType
	{
		get;
		set
		{
			if (value.Equals(field))
				return;
			field = value;
			NotifyOfPropertyChange(() => MessageBoxType);
		}
	}

	/// <summary>
	///     Show the No button.
	/// </summary>
	public bool IsNoButtonVisible => Buttons is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel;

	/// <summary>
	///     Show the Yes button.
	/// </summary>
	public bool IsYesButtonVisible => Buttons is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel;

	/// <summary>
	///     Show the Cancel button.
	/// </summary>
	public bool IsCancelButtonVisible => Buttons is MessageBoxButton.OKCancel or MessageBoxButton.YesNoCancel;

	/// <summary>
	///     Show the Ok button.
	/// </summary>
	public bool IsOkButtonVisible => Buttons is MessageBoxButton.OK or MessageBoxButton.OKCancel;

	/// <summary>
	///     The MessageBox message.
	/// </summary>
	public string Message
	{
		get;
		set
		{
			if (value.Equals(field))
				return;
			field = value;
			NotifyOfPropertyChange(() => Message);
		}
	}

	/// <summary>
	///     The MessageBox available buttons.
	/// </summary>
	public MessageBoxButton Buttons
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => IsNoButtonVisible);
			NotifyOfPropertyChange(() => IsYesButtonVisible);
			NotifyOfPropertyChange(() => IsCancelButtonVisible);
			NotifyOfPropertyChange(() => IsOkButtonVisible);
		}
	} = MessageBoxButton.OK;

	/// <summary>
	///     Return value of the MessageBox.
	/// </summary>
	public MessageBoxResult Result { get; private set; }

	/// <summary>
	///     Manage click of No button.
	/// </summary>
	public void No()
	{
		Result = MessageBoxResult.No;
		TryCloseAsync(false);
	}

	/// <summary>
	///     Manage click of Yes button.
	/// </summary>
	public void Yes()
	{
		Result = MessageBoxResult.Yes;
		TryCloseAsync(true);
	}

	/// <summary>
	///     Manage click of Cancel button.
	/// </summary>
	public void Cancel()
	{
		Result = MessageBoxResult.Cancel;
		TryCloseAsync(false);
	}

	/// <summary>
	///     Manage click of Ok button.
	/// </summary>
	public void Ok()
	{
		Result = MessageBoxResult.OK;
		TryCloseAsync(true);
	}
}
