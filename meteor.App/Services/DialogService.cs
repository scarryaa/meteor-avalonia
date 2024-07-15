using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using meteor.App.Dialogs;
using meteor.Core.Enums;
using meteor.Core.Interfaces;

namespace meteor.App.Services;

public class DialogService(Func<Window?> getMainWindow) : IDialogService
{
    private Func<Window?> _getMainWindow;

    public async Task<DialogResult> ShowConfirmationDialogAsync(string message, string title, string yesText,
        string noText, string cancelText)
    {
        _getMainWindow = getMainWindow;
        
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = yesText,
            SecondaryButtonText = noText,
            CloseButtonText = cancelText
        };

        var result = await dialog.ShowAsync();

        return result switch
        {
            ContentDialogResult.Primary => DialogResult.Yes,
            ContentDialogResult.Secondary => DialogResult.No,
            _ => DialogResult.Cancel
        };
    }

    public async Task ShowErrorDialogAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            PrimaryButtonText = "OK"
        };

        await dialog.ShowAsync();
    }
}