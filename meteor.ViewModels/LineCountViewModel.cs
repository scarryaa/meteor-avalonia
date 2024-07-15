using System.ComponentModel;
using System.Runtime.CompilerServices;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.ViewModels;

public class LineCountViewModel : ILineCountViewModel
{
    private int _lineCount = 1;
    private double _verticalOffset;
    private double _viewportHeight;
    private int _maxLineNumber = 1;

    public int LineCount
    {
        get => Math.Max(_lineCount, 1);
        set
        {
            if (SetField(ref _lineCount, value)) UpdateMaxLineNumber();
        }
    }

    public double VerticalOffset
    {
        get => _verticalOffset;
        set => SetField(ref _verticalOffset, value);
    }

    public double ViewportHeight
    {
        get => _viewportHeight;
        set => SetField(ref _viewportHeight, value);
    }

    public int MaxLineNumber
    {
        get => _maxLineNumber;
        private set => SetField(ref _maxLineNumber, value);
    }

    private void UpdateMaxLineNumber()
    {
        MaxLineNumber = LineCount;
    }

    public void UpdateLineCount(int newLineCount)
    {
        LineCount = Math.Max(newLineCount, 1);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}