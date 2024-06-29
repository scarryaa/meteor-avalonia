using System.Numerics;
using ReactiveUI;

namespace meteor.ViewModels;

public class LineCountViewModel : ReactiveObject
{
    private BigInteger _lineCount = 1;
    private double _verticalOffset;
    private double _viewportHeight;
    private BigInteger _maxLineNumber;

    public BigInteger LineCount
    {
        get => BigInteger.Max(_lineCount, 1);
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

    public BigInteger MaxLineNumber
    {
        get => _maxLineNumber;
        private set => this.RaiseAndSetIfChanged(ref _maxLineNumber, value);
    }

    private void UpdateMaxLineNumber()
    {
        MaxLineNumber = LineCount > 0 ? LineCount : 1;
    }

    public void UpdateLineCount(BigInteger newLineCount)
    {
        LineCount = BigInteger.Max(newLineCount, 1);
    }
}