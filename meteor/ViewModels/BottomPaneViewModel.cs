using ReactiveUI;

namespace meteor.ViewModels;

public class BottomPaneViewModel : ReactiveObject
{
    private double _bottomPaneHeight = 150; // Initial height

    public double BottomPaneHeight
    {
        get => _bottomPaneHeight;
        set => this.RaiseAndSetIfChanged(ref _bottomPaneHeight, value);
    }
}