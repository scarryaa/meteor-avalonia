using ReactiveUI;

namespace meteor.ViewModels;

public class BottomPaneViewModel : ReactiveObject
{
    private double _bottomPaneHeight;

    public double BottomPaneHeight
    {
        get => _bottomPaneHeight;
        set => this.RaiseAndSetIfChanged(ref _bottomPaneHeight, value);
    }

    public BottomPaneViewModel()
    {
        _bottomPaneHeight = 150; // Initial height
    }
}