using System;
using Avalonia.Media;
using meteor.Interfaces;
using meteor.Views.Services;
using ReactiveUI;

namespace meteor.ViewModels;

public class GutterViewModel : ViewModelBase, IDisposable
{
    private readonly FontPropertiesViewModel _fontPropertiesViewModel;
    private readonly IThemeService _themeService;
    private FontFamily _fontFamily;
    private double _fontSize;
    private double _lineHeight;
    private IBrush _backgroundBrush;
    private IBrush _foregroundBrush;
    private IBrush _lineHighlightBrush;
    private IBrush _selectedBrush;
    private TextEditorViewModel _textEditorViewModel;
    private ScrollableTextEditorViewModel _scrollableTextEditorViewModel;
    private ScrollManager _scrollManager;

    public TextEditorViewModel TextEditorViewModel
    {
        get => _textEditorViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _textEditorViewModel, value);
            if (_textEditorViewModel != null && InvalidateRequired != null)
                _textEditorViewModel.WhenAnyValue(te => te.CursorPosition)
                    .Subscribe(cursorPosition =>
                    {
                        CursorPosition = cursorPosition;
                        InvalidateRequired?.Invoke(this, EventArgs.Empty);
                    });
        }
    }

    public ScrollManager ScrollManager
    {
        get => _scrollManager;
        set
        {
            this.RaiseAndSetIfChanged(ref _scrollManager, value);
            OnScrollManagerChanged();
        }
    }

    public LineCountViewModel LineCountViewModel { get; }

    public ScrollableTextEditorViewModel ScrollableTextEditorViewModel
    {
        get => _scrollableTextEditorViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _scrollableTextEditorViewModel, value);
            if (_scrollableTextEditorViewModel != null)
                _scrollableTextEditorViewModel.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(ScrollableTextEditorViewModel.HorizontalOffset))
                        OnInvalidateRequired();
                };
        }
    }

    private void OnTextEditorSelectionChanged(object sender, EventArgs e)
    {
        OnInvalidateRequired();
    }

    public long CursorPosition { get; set; }

    public FontFamily FontFamily
    {
        get => _fontPropertiesViewModel.FontFamily;
        set
        {
            if (_fontPropertiesViewModel.FontFamily != value)
            {
                _fontPropertiesViewModel.FontFamily = value;
                OnInvalidateRequired();
            }
        }
    }

    public double FontSize
    {
        get => _fontPropertiesViewModel.FontSize;
        set
        {
            if (_fontPropertiesViewModel.FontSize != value)
            {
                _fontPropertiesViewModel.FontSize = value;
                OnInvalidateRequired();
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
                _fontPropertiesViewModel.LineHeight = value;
                this.RaiseAndSetIfChanged(ref _lineHeight, value);
            }
        }
    }

    public IBrush BackgroundBrush
    {
        get => _backgroundBrush;
        set => this.RaiseAndSetIfChanged(ref _backgroundBrush, value);
    }

    public IBrush ForegroundBrush
    {
        get => _foregroundBrush;
        set => this.RaiseAndSetIfChanged(ref _foregroundBrush, value);
    }

    public IBrush LineHighlightBrush
    {
        get => _lineHighlightBrush;
        set => this.RaiseAndSetIfChanged(ref _lineHighlightBrush, value);
    }

    public IBrush SelectedBrush
    {
        get => _selectedBrush;
        set => this.RaiseAndSetIfChanged(ref _selectedBrush, value);
    }

    public double VerticalOffset
    {
        get => LineCountViewModel.VerticalOffset;
        set => LineCountViewModel.VerticalOffset = value;
    }

    public double ViewportHeight
    {
        get => LineCountViewModel.ViewportHeight;
        set => LineCountViewModel.ViewportHeight = value;
    }

    public long LineCount
    {
        get => LineCountViewModel.LineCount;
        set => LineCountViewModel.LineCount = value;
    }

    public GutterViewModel(
        ICursorPositionService cursorPositionService,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel,
        ScrollableTextEditorViewModel scrollableTextEditorViewModel,
        TextEditorViewModel textEditorViewModel,
        IThemeService themeService)
    {
        _themeService = themeService;
        _fontPropertiesViewModel = fontPropertiesViewModel;
        LineCountViewModel = lineCountViewModel;
        TextEditorViewModel = textEditorViewModel;
        _scrollableTextEditorViewModel = scrollableTextEditorViewModel;

        cursorPositionService.CursorPositionChanged += (cursorPosition, _) =>
        {
            CursorPosition = cursorPosition;
            OnInvalidateRequired();
        };

        TextEditorViewModel.SelectionChanged += OnTextEditorSelectionChanged;

        _fontPropertiesViewModel.WhenAnyValue(x => x.LineHeight)
            .Subscribe(lineHeight => LineHeight = lineHeight);

        _fontPropertiesViewModel.WhenAnyValue(x => x.FontFamily)
            .Subscribe(fontFamily => FontFamily = fontFamily);

        _fontPropertiesViewModel.WhenAnyValue(x => x.FontSize)
            .Subscribe(fontSize => FontSize = fontSize);

        _lineHeight = _fontPropertiesViewModel.LineHeight;

        // Subscribe to VerticalOffset changes
        LineCountViewModel.WhenAnyValue(x => x.VerticalOffset)
            .Subscribe(offset => OnInvalidateRequired());

        // Subscribe to ViewportHeight changes
        LineCountViewModel.WhenAnyValue(x => x.ViewportHeight)
            .Subscribe(height => OnInvalidateRequired());

        // Subscribe to LineCount changes
        LineCountViewModel.WhenAnyValue(x => x.LineCount)
            .Subscribe(count => OnInvalidateRequired());

        TextEditorViewModel.WidthChanged += (sender, args) => { OnWidthRecalculationRequired(); };

        TextEditorViewModel.LineChanged += (sender, args) =>
        {
            CursorPosition = TextEditorViewModel.CursorPosition;
            OnInvalidateRequired();
        };

        _themeService.ThemeChanged += OnThemeChanged;

        TextEditorViewModel.PropertyChanged += (_, _) => InvalidateRequired?.Invoke(this, EventArgs.Empty);
        LineCountViewModel.PropertyChanged += (_, _) => InvalidateRequired?.Invoke(this, EventArgs.Empty);

        UpdateBrushes();
    }

    private void OnThemeChanged(object sender, EventArgs e)
    {
        UpdateBrushes();
    }

    private void UpdateBrushes()
    {
        BackgroundBrush = GetResourceBrush("GutterBackground");
        ForegroundBrush = GetResourceBrush("GutterDefault");
        LineHighlightBrush = GetResourceBrush("GutterHighlight");
        SelectedBrush = GetResourceBrush("GutterSelected");

        OnInvalidateRequired();
    }

    public event EventHandler? InvalidateRequired;
    public event EventHandler? ScrollManagerChanged;
    public event EventHandler? WidthRecalculationRequired;

    public virtual void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnScrollManagerChanged()
    {
        ScrollManagerChanged?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnWidthRecalculationRequired()
    {
        WidthRecalculationRequired?.Invoke(this, EventArgs.Empty);
    }

    private IBrush GetResourceBrush(string key)
    {
        return _themeService.GetResourceBrush(key) ?? Brushes.Transparent;
    }

    public void Dispose()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        TextEditorViewModel.SelectionChanged -= OnTextEditorSelectionChanged;
    }
}
