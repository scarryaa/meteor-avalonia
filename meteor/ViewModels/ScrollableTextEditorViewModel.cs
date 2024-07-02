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
    private double _totalHeight;
    private Vector _offset;
    private double _lineHeight;
    private bool _disableHorizontalScrollToCursor;
    private bool _disableVerticalScrollToCursor;
    private double _windowHeight;
    private double _windowWidth;

    public TextEditorViewModel TextEditorViewModel { get; }
    public LineCountViewModel LineCountViewModel { get; }
    public GutterViewModel GutterViewModel { get; }
    public TabViewModel TabViewModel { get; set; }

    public bool DisableHorizontalScrollToCursor
    {
        get => _disableHorizontalScrollToCursor;
        set => this.RaiseAndSetIfChanged(ref _disableHorizontalScrollToCursor, value);
    }

    public bool DisableVerticalScrollToCursor
    {
        get => _disableVerticalScrollToCursor;
        set => this.RaiseAndSetIfChanged(ref _disableHorizontalScrollToCursor, value);
    }

    public ScrollableTextEditorViewModel(
        ICursorPositionService cursorPositionService,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel,
        ITextBuffer textBuffer,
        IClipboardService clipboardService,
        IThemeService themeService)
    {
        ClipboardService = clipboardService;
        
        if (textBuffer == null) throw new ArgumentNullException(nameof(textBuffer));

        FontPropertiesViewModel = fontPropertiesViewModel;
        LineCountViewModel = lineCountViewModel;
        TextEditorViewModel = new TextEditorViewModel(
            cursorPositionService,
            fontPropertiesViewModel,
            lineCountViewModel,
            textBuffer,
            clipboardService);
        GutterViewModel = new GutterViewModel(cursorPositionService, fontPropertiesViewModel, lineCountViewModel, this,
            TextEditorViewModel, themeService);

        LineHeight = FontSize * 1.5;

        this.WhenAnyValue(x => x.LineCountViewModel.VerticalOffset)
            .Subscribe(verticalOffset => VerticalOffset = verticalOffset);

        this.WhenAnyValue(fp => fp.FontPropertiesViewModel.LineHeight)
            .Subscribe(lineHeight =>
            {
                textBuffer.LineHeight = TextEditorViewModel.FontPropertiesViewModel.CalculateLineHeight(FontSize);
                TextEditorViewModel.LineHeight = lineHeight;
            });

        textBuffer.TextChanged += (sender, args) => UpdateDimensions();
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
        get => _lineHeight;
        set
        {
            if (_lineHeight != value)
            {
                this.RaiseAndSetIfChanged(ref _lineHeight, value);
                TextEditorViewModel.TextBuffer.LineHeight = value;
            }
        }
    }

    public double LongestLineWidth
    {
        get => Math.Max(_longestLineWidth + 20, WindowWidth);
        set
        {
            if (_longestLineWidth != value)
                this.RaiseAndSetIfChanged(ref _longestLineWidth, value);
        }
    }

    public double TotalHeight
    {
        get => Math.Max(_totalHeight, _windowHeight);
        set
        {
            if (_totalHeight != value) this.RaiseAndSetIfChanged(ref _totalHeight, value);
        }
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set
        {
            this.RaiseAndSetIfChanged(ref _windowHeight, value);
            TotalHeight = Math.Max(_totalHeight, _windowHeight);
        }
    }

    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            this.RaiseAndSetIfChanged(ref _windowWidth, value);
            LongestLineWidth = Math.Max(_windowWidth, _longestLineWidth);
        }
    }

    public IClipboardService ClipboardService { get; set; }

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
                UpdateTotalHeight();
            }
        }
    }

    public void UpdateLongestLineWidth()
    {
        var longestLineLength = TextEditorViewModel.TextBuffer.LongestLineLength;
        var charWidth = TextEditorViewModel.CharWidth;
        var calculatedWidth = longestLineLength * charWidth;

        LongestLineWidth = calculatedWidth;
    }

    public void UpdateTotalHeight()
    {
        var totalHeight = TextEditorViewModel.TextBuffer.TotalHeight;
        TotalHeight = totalHeight;
    }

    public void UpdateDimensions()
    {
        this.RaisePropertyChanged(nameof(Viewport));
        this.RaisePropertyChanged(nameof(Offset));
        this.RaisePropertyChanged(nameof(VerticalOffset));
        this.RaisePropertyChanged(nameof(HorizontalOffset));
        
        UpdateLongestLineWidth();
        UpdateTotalHeight();
    }

    public void UpdateViewProperties()
    {
        UpdateProperties();
    }

    private void UpdateProperties()
    {
        this.RaisePropertyChanged(nameof(FontFamily));
        this.RaisePropertyChanged(nameof(FontSize));
        this.RaisePropertyChanged(nameof(LineHeight));
        this.RaisePropertyChanged(nameof(LongestLineWidth));
        this.RaisePropertyChanged(nameof(VerticalOffset));
        this.RaisePropertyChanged(nameof(HorizontalOffset));
        this.RaisePropertyChanged(nameof(Offset));
        this.RaisePropertyChanged(nameof(Viewport));

        TextEditorViewModel.RaisePropertyChanged(nameof(TextEditorViewModel.FontFamily));
        TextEditorViewModel.RaisePropertyChanged(nameof(TextEditorViewModel.FontSize));
        TextEditorViewModel.RaisePropertyChanged(nameof(TextEditorViewModel.LineHeight));
        TextEditorViewModel.RaisePropertyChanged(nameof(TextEditorViewModel.WindowHeight));
        TextEditorViewModel.RaisePropertyChanged(nameof(TextEditorViewModel.WindowWidth));
        TextEditorViewModel.RaisePropertyChanged(nameof(TextEditorViewModel.TextBuffer));

        TextEditorViewModel.WindowWidth = WindowWidth;
        TextEditorViewModel.WindowHeight = WindowHeight;
        TextEditorViewModel.OnInvalidateRequired();
        UpdateDimensions();

        TextEditorViewModel.RaisePropertyChanged(nameof(TextEditorViewModel.TotalHeight));

        GutterViewModel.OnInvalidateRequired();
    }
}