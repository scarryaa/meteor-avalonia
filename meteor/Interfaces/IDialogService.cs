using System.Threading.Tasks;
using meteor.Enums;
using meteor.ViewModels;

namespace meteor.Interfaces;

public interface IDialogService
{
    Task<ContentDialogResult> ShowContentDialogAsync(
        MainWindowViewModel mainWindowViewModel,
        string title,
        string content,
        string primaryButtonText,
        string secondaryButtonText,
        string closeButtonText);

    Task ShowErrorDialogAsync(MainWindowViewModel mainWindowViewModel, string message);
}