using System;
using System.Threading.Tasks;

namespace meteor.App.Dialogs;

public enum ContentDialogResult
{
    None,
    Primary,
    Secondary
}

public class ContentDialog
{
    public string Title { get; set; }
    public object Content { get; set; }
    public string PrimaryButtonText { get; set; }
    public string SecondaryButtonText { get; set; }
    public string CloseButtonText { get; set; }

    public Task<ContentDialogResult> ShowAsync()
    {
        return Task.FromResult(SimulateUserInput());
    }

    private ContentDialogResult SimulateUserInput()
    {
        var random = new Random();
        var choice = random.Next(3);
        return (ContentDialogResult)choice;
    }
}