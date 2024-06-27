using Avalonia;
using ReactiveUI;

namespace meteor.ViewModels;

public class ScrollableTextEditorViewModel : ViewModelBase
{
    private double _verticalOffset;
    private double _horizontalOffset;
    private Size _viewport;
    private double _longestLineWidth;

    public TextEditorViewModel TextEditorViewModel { get; } = new();

    public double LongestLineWidth
    {
        get => _longestLineWidth;
        set
        {
            if (_longestLineWidth != value) this.RaiseAndSetIfChanged(ref _longestLineWidth, value);
        }
    }
    
    public double VerticalOffset
    {
        get => _verticalOffset;
        set
        {
            if (_verticalOffset != value) this.RaiseAndSetIfChanged(ref _verticalOffset, value);
        }
    }

    public double HorizontalOffset
    {
        get => _horizontalOffset;
        set
        {
            if (_horizontalOffset != value) this.RaiseAndSetIfChanged(ref _horizontalOffset, value);
        }
    }

    public Size Viewport
    {
        get => _viewport;
        set
        {
            if (_viewport != value) this.RaiseAndSetIfChanged(ref _viewport, value);
        }
    }
}