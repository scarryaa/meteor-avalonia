using System.ComponentModel;
using System.Runtime.CompilerServices;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;

namespace meteor.ViewModels;

public class ScrollableTextEditorViewModel : IScrollableTextEditorViewModel
{
    private double _verticalOffset;
    private double _horizontalOffset;
    private double _viewportWidth;
    private double _viewportHeight;
    private double _longestLineWidth;
    private double _totalHeight;
    private double _lineHeight;
    private double _windowHeight;
    private double _windowWidth;
    private Vector _offset;

    public ITextEditorViewModel TextEditorViewModel { get; }
    public ILineCountViewModel LineCountViewModel { get; }
    public IGutterViewModel GutterViewModel { get; }

    public double RequiredWidth => Math.Max(LongestLineWidth, ViewportWidth);
    public double RequiredHeight => Math.Max(TotalHeight, ViewportHeight);

    public ScrollableTextEditorViewModel(
        ITextEditorViewModel textEditorViewModel,
        ILineCountViewModel lineCountViewModel,
        IGutterViewModel gutterViewModel)
    {
        TextEditorViewModel = textEditorViewModel;
        LineCountViewModel = lineCountViewModel;
        GutterViewModel = gutterViewModel;

        TextEditorViewModel.PropertyChanged += TextEditorViewModel_PropertyChanged;
        LineHeight = TextEditorViewModel.FontSize * 1.5;
    }

    public double FontSize
    {
        get => TextEditorViewModel.FontSize;
        set
        {
            if (TextEditorViewModel.FontSize != value)
            {
                TextEditorViewModel.FontSize = value;
                OnPropertyChanged();
                LineHeight = value * 1.5;
            }
        }
    }

    public double LineHeight
    {
        get => _lineHeight;
        set
        {
            if (_lineHeight != value)
            {
                _lineHeight = value;
                OnPropertyChanged();
                UpdateTotalHeight();
            }
        }
    }

    public double LongestLineWidth
    {
        get => Math.Max(_longestLineWidth, WindowWidth);
        private set
        {
            if (_longestLineWidth != value)
            {
                _longestLineWidth = value;
                OnPropertyChanged();
            }
        }
    }

    public double TotalHeight
    {
        get => Math.Max(_totalHeight, WindowHeight);
        private set
        {
            if (_totalHeight != value)
            {
                _totalHeight = value;
                OnPropertyChanged();
            }
        }
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set
        {
            if (_windowHeight != value)
            {
                _windowHeight = value;
                OnPropertyChanged();
                UpdateTotalHeight();
            }
        }
    }

    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            if (_windowWidth != value)
            {
                _windowWidth = value;
                OnPropertyChanged();
                UpdateLongestLineWidth();
            }
        }
    }

    public double ViewportHeight
    {
        get => _viewportHeight;
        set
        {
            if (_viewportHeight != value)
            {
                _viewportHeight = value;
                OnPropertyChanged();
                UpdateTotalHeight();
            }
        }
    }

    public double ViewportWidth
    {
        get => _viewportWidth;
        set
        {
            if (_viewportWidth != value)
            {
                _viewportWidth = value;
                OnPropertyChanged();
                UpdateLongestLineWidth();
            }
        }
    }

    public Vector Offset
    {
        get => _offset;
        set
        {
            if (_offset != value)
            {
                _offset = value;
                OnPropertyChanged();
                VerticalOffset = value.Y;
                HorizontalOffset = value.X;
            }
        }
    }
    
    public double VerticalOffset
    {
        get => _offset.Y;
        set
        {
            if (_offset.Y != value)
            {
                _offset = (Vector)_offset.WithY(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Offset));
                LineCountViewModel.VerticalOffset = value;
            }
        }
    }

    public double HorizontalOffset
    {
        get => _offset.X;
        set
        {
            if (_offset.X != value)
            {
                _offset = (Vector)_offset.WithX(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Offset));
            }
        }
    }

    public void UpdateViewProperties()
    {
        TextEditorViewModel.UpdateLineStarts();
        UpdateLongestLineWidth();
        UpdateTotalHeight();
    }


    private void UpdateLongestLineWidth()
    {
        var longestLineLength = TextEditorViewModel.LongestLineLength;
        var charWidth = TextEditorViewModel.CharWidth;
        var calculatedWidth = longestLineLength * charWidth;

        LongestLineWidth = calculatedWidth;
        OnPropertyChanged(nameof(RequiredWidth));
    }

    private void UpdateTotalHeight()
    {
        if (TextEditorViewModel?.TextBuffer == null)
        {
            TotalHeight = 0;
            OnPropertyChanged(nameof(RequiredHeight));
            return;
        }

        var lineCount = TextEditorViewModel.TextBuffer.LineCount;
        TotalHeight = lineCount * LineHeight;
        OnPropertyChanged(nameof(RequiredHeight));
    }

    private void TextEditorViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ITextEditorViewModel.TextBuffer):
            case nameof(ITextEditorViewModel.LongestLineLength):
                UpdateViewProperties();
                break;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}