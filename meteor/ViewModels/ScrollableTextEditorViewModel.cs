using System;
using Avalonia;
using Avalonia.Media;
using meteor.Interfaces;
using meteor.Views.Services;
using ReactiveUI;

namespace meteor.ViewModels;

public class ScrollableTextEditorViewModel : ViewModelBase
{
    private const double ScrollThreshold = 20;
    private const double ScrollSpeed = 1;

    private double _verticalOffset;
    private double _horizontalOffset;
    private Size _viewport;
    private double _longestLineWidth;
    private double _totalHeight;
    private Vector _offset;
    private double _lineHeight;
    private double _windowHeight;
    private double _windowWidth;
    private ScrollManager _scrollManager;
    private double _savedVerticalOffset;
    private double _savedHorizontalOffset;

    public TextEditorViewModel TextEditorViewModel { get; }
    public LineCountViewModel LineCountViewModel { get; }
    public GutterViewModel GutterViewModel { get; }
    public TabViewModel TabViewModel { get; set; }
    public RenderManager ParentRenderManager { get; set; }

    public ScrollManager ScrollManager
    {
        get => _scrollManager;
        set
        {
            this.RaiseAndSetIfChanged(ref _scrollManager, value);
            GutterViewModel.ScrollManager = value;
        }
    }

    public ScrollableTextEditorViewModel(
        ICursorPositionService cursorPositionService,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel,
        ITextBuffer textBuffer,
        IClipboardService clipboardService,
        IThemeService themeService,
        TextEditorViewModel textEditorViewModel,
        ScrollManager scrollManager)
    {
        ClipboardService = clipboardService;
        TextEditorViewModel = textEditorViewModel;
        TextEditorViewModel.ParentViewModel = this;
        GutterViewModel = new GutterViewModel(cursorPositionService, fontPropertiesViewModel, lineCountViewModel,
            this,
            TextEditorViewModel, themeService);
        ScrollManager = scrollManager;
        GutterViewModel.ScrollManager = ScrollManager;

        if (textBuffer == null) throw new ArgumentNullException(nameof(textBuffer));

        FontPropertiesViewModel = fontPropertiesViewModel;
        LineCountViewModel = lineCountViewModel;

        LineHeight = FontSize * 1.5;

        this.WhenAnyValue(x => x.LineCountViewModel.VerticalOffset)
            .Subscribe(verticalOffset => VerticalOffset = verticalOffset);

        this.WhenAnyValue(fp => fp.FontPropertiesViewModel.LineHeight)
            .Subscribe(lineHeight => { LineHeight = lineHeight; });

        textBuffer.TextChanged += (sender, args) =>
        {
            UpdateDimensions();
            UpdateProperties();
        };

        // Subscribe to property changes
        this.WhenAnyValue(
                x => x.VerticalOffset,
                x => x.HorizontalOffset,
                x => x.Viewport,
                x => x.Offset,
                x => x.LineHeight)
            .Subscribe(_ => UpdateContext());

        this.WhenAnyValue(
                x => x.LongestLineWidth,
                x => x.TotalHeight,
                x => x.WindowHeight,
                x => x.WindowWidth,
                x => x.FontFamily,
                x => x.FontSize)
            .Subscribe(_ => UpdateContext());
    }
    
    private void UpdateContext()
    {
        ParentRenderManager?.UpdateContextViewModel(this);
    }

    public void UpdateViewProperties()
    {
        TextEditorViewModel.UpdateLineStarts();
        UpdateLongestLineWidth();
        UpdateTotalHeight();
        UpdateScrollOffsets();
    }

    public void UpdateScrollOffsets()
    {
        VerticalOffset = SavedVerticalOffset;
        HorizontalOffset = SavedHorizontalOffset;
    }

    public void UpdateLineStarts()
    {
        TextEditorViewModel.TextBuffer.UpdateLineCache();
        this.RaisePropertyChanged(nameof(LineCountViewModel));
        this.RaisePropertyChanged(nameof(TotalHeight));
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
                TextEditorViewModel.LineHeight = value;
                UpdateTotalHeight();
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

    public double SavedVerticalOffset
    {
        get => _savedVerticalOffset;
        set => this.RaiseAndSetIfChanged(ref _savedVerticalOffset, value);
    }

    public double SavedHorizontalOffset
    {
        get => _savedHorizontalOffset;
        set => this.RaiseAndSetIfChanged(ref _savedHorizontalOffset, value);
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
        var totalHeight = Math.Ceiling(TextEditorViewModel.TextBuffer.TotalHeight);
        TotalHeight = Math.Max(totalHeight, WindowHeight);
    }

    public void UpdateDimensions()
    {
        UpdateLongestLineWidth();
        UpdateTotalHeight();
        this.RaisePropertyChanged(nameof(Viewport));
        this.RaisePropertyChanged(nameof(Offset));
        this.RaisePropertyChanged(nameof(VerticalOffset));
        this.RaisePropertyChanged(nameof(HorizontalOffset));
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
        TextEditorViewModel.RaisePropertyChanged(nameof(TextEditorViewModel.LongestLineWidth));

        GutterViewModel.OnInvalidateRequired();

        Console.WriteLine("TextEditorViewModel properties updated");
    }
}