using meteor.Core.Enums;

namespace meteor.Core.Interfaces;

public interface IDialogService
{
    Task<DialogResult> ShowConfirmationDialogAsync(string message, string title, string yesText, string noText,
        string cancelText);

    Task ShowErrorDialogAsync(string message);
}