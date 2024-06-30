using ReactiveUI;

namespace meteor.ViewModels;

public class LineCountViewModel : ReactiveObject
{
    private long _lineCount = 1;
    private double _verticalOffset;
    private double _viewportHeight;
    private long _maxLineNumber;

    public long LineCount
    {
        get => long.Max(_lineCount, 1);
        set => this.RaiseAndSetIfChanged(ref _lineCount, value);
    }

    public double VerticalOffset
    {
        get => _verticalOffset;
        set => this.RaiseAndSetIfChanged(ref _verticalOffset, value);
    }

    public double ViewportHeight
    {
        get => _viewportHeight;
        set => this.RaiseAndSetIfChanged(ref _viewportHeight, value);
    }

    public long MaxLineNumber
    {
        get => _maxLineNumber;
        set => this.RaiseAndSetIfChanged(ref _maxLineNumber, value);
    }

    private void UpdateMaxLineNumber()
    {
        MaxLineNumber = LineCount > 0 ? LineCount : 1;
    }

    public void UpdateLineCount(long newLineCount)
    {
        LineCount = long.Max(newLineCount, 1);
    }
}