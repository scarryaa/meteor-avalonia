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
    private FontFamily _fontFamily;
    private double _fontSize;

    public TextEditorViewModel TextEditorViewModel { get; }

    public ScrollableTextEditorViewModel(ICursorPositionService cursorPositionService)
    {
        _fontFamily = new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono");
        _fontSize = 13;
        TextEditorViewModel = new TextEditorViewModel(cursorPositionService);

        this.WhenAnyValue(x => x.FontFamily)
            .Subscribe(font => TextEditorViewModel.FontFamily = font);
        this.WhenAnyValue(x => x.FontSize)
            .Subscribe(size => TextEditorViewModel.FontSize = size);
    }

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set => this.RaiseAndSetIfChanged(ref _fontFamily, value);
    }

    public double FontSize
    {
        get => _fontSize;
        set => this.RaiseAndSetIfChanged(ref _fontSize, value);
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
            if (_viewport != value) this.RaiseAndSetIfChanged(ref _viewport, value);
        }
    }
}