using ReactiveUI;

namespace meteor.ViewModels;

public class TitleBarViewModel : ViewModelBase
{
    private string? _openProjectName;

    public string? OpenProjectName
    {
        get => _openProjectName;
        set => this.RaiseAndSetIfChanged(ref _openProjectName, value);
    }
}