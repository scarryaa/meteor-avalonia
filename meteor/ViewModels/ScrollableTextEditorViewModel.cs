using System;
using Avalonia;
using Avalonia.Media;
using meteor.Interfaces;
using ReactiveUI;

namespace meteor.ViewModels;

public class ScrollableTextEditorViewModel : ViewModelBase
{
    private double _verticalOffset;
    private double _horizontalOffset;
    private Size _viewport;
    private double _longestLineWidth;
    private Vector _offset;
    private double _lineHeight;
    private bool _disableHorizontalScrollToCursor;

    public TextEditorViewModel TextEditorViewModel { get; }
    public LineCountViewModel LineCountViewModel { get; }

    public bool DisableHorizontalScrollToCursor
    {
        get => _disableHorizontalScrollToCursor;
        set => this.RaiseAndSetIfChanged(ref _disableHorizontalScrollToCursor, value);
    }
    
    public ScrollableTextEditorViewModel(
        ICursorPositionService cursorPositionService,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel)
    {
        FontPropertiesViewModel = fontPropertiesViewModel;
        LineCountViewModel = lineCountViewModel;
        TextEditorViewModel =
            new TextEditorViewModel(cursorPositionService, fontPropertiesViewModel, lineCountViewModel);

        // Subscribe to changes in line count
        this.WhenAnyValue(x => x.LineCountViewModel.LineCount)
            .Subscribe(count => LongestLineWidth = Math.Max(LongestLineWidth, (double)count * LineHeight));

        this.WhenAnyValue(x => x.LineCountViewModel.VerticalOffset)
            .Subscribe(verticalOffset => VerticalOffset = verticalOffset);
    }

    public FontPropertiesViewModel FontPropertiesViewModel { get; }

    public FontFamily FontFamily
    {
        get => FontPropertiesViewModel.FontFamily;
        set => FontPropertiesViewModel.FontFamily = value;
    }

    public double FontSize
    {
        get => FontPropertiesViewModel.FontSize;
        set => FontPropertiesViewModel.FontSize = value;
    }

    public double LineHeight
    {
        get => FontPropertiesViewModel.LineHeight;
        set => this.RaiseAndSetIfChanged(ref _lineHeight, value);
    }

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
            if (_verticalOffset != value)
            {
                this.RaiseAndSetIfChanged(ref _verticalOffset, value);
                Offset = new Vector(Offset.X, _verticalOffset);
                LineCountViewModel.VerticalOffset = Offset.Y;
            }
        }
    }

    public double HorizontalOffset
    {
        get => _horizontalOffset;
        set
        {
            if (_horizontalOffset != value)
            {
                this.RaiseAndSetIfChanged(ref _horizontalOffset, value);
                Offset = new Vector(_horizontalOffset, Offset.Y);
            }
        }
    }

    public Vector Offset
    {
        get => _offset;
        set
        {
            if (_offset != value) this.RaiseAndSetIfChanged(ref _offset, value);
        }
    }

    public Size Viewport
    {
        get => _viewport;
        set
        {
            if (_viewport != value)
            {
                this.RaiseAndSetIfChanged(ref _viewport, value);
                LineCountViewModel.ViewportHeight = value.Height;
            }
        }
    }
}
