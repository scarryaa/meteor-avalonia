using meteor.Enums;
using meteor.Interfaces;
using meteor.Services;
using meteor.ViewModels;

namespace tests.Services;

public class TestDialogService : DialogService
{
    public TestDialogService(IMainWindowProvider mainWindowProvider)
        : base(mainWindowProvider)
    {
    }

    public ContentDialogResult SimulatedResult { get; set; }
    public TaskCompletionSource<bool> DialogOpenedTcs { get; } = new();

    protected override void SetDialogOpenState(MainWindowViewModel mainWindowViewModel, bool isOpen)
    {
        if (isOpen)
            DialogOpenedTcs.TrySetResult(true);
        else
            DialogOpenedTcs.TrySetResult(false);
        base.SetDialogOpenState(mainWindowViewModel, isOpen);
    }

    protected override ContentDialogResult ShowConsoleFallbackDialog(string title, string content)
    {
        return SimulatedResult;
    }
}