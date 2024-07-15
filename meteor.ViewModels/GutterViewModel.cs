using System.ComponentModel;
using System.Runtime.CompilerServices;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Events;
using Microsoft.Extensions.Logging;

namespace meteor.ViewModels;

public class GutterViewModel : IGutterViewModel, INotifyPropertyChanged, IDisposable
{
    private readonly IThemeService _themeService;
    private readonly Lazy<ITextEditorViewModel> _textEditorViewModel;
    private double _lineHeight;
    private IScrollManager _scrollManager;
    private readonly ILogger<GutterViewModel> _logger;

    public ILineCountViewModel LineCountViewModel { get; }
    public int CursorPosition { get; set; }
    public string FontFamily { get; set; }
    public double FontSize { get; set; }

    public double LineHeight
    {
        get => _lineHeight;
        set => SetProperty(ref _lineHeight, value);
    }

    private object _backgroundBrush;

    public object BackgroundBrush
    {
        get => _backgroundBrush;
        set => SetProperty(ref _backgroundBrush, value);
    }

    private object _foregroundBrush;

    public object ForegroundBrush
    {
        get => _foregroundBrush;
        set => SetProperty(ref _foregroundBrush, value);
    }

    private object _lineHighlightBrush;

    public object LineHighlightBrush
    {
        get => _lineHighlightBrush;
        set => SetProperty(ref _lineHighlightBrush, value);
    }

    private object _selectedBrush;

    public object SelectedBrush
    {
        get => _selectedBrush;
        set => SetProperty(ref _selectedBrush, value);
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

    public int LineCount
    {
        get => LineCountViewModel.LineCount;
        set => LineCountViewModel.LineCount = value;
    }

    public IScrollManager ScrollManager
    {
        get => _scrollManager;
        set => SetProperty(ref _scrollManager, value);
    }

    public GutterViewModel(
        ICursorPositionService cursorPositionService,
        ILineCountViewModel lineCountViewModel,
        Lazy<ITextEditorViewModel> textEditorViewModel,
        IThemeService themeService,
        ILogger<GutterViewModel> logger)
    {
        _logger = logger;
        _logger.LogDebug("Initializing GutterViewModel");
        
        _themeService = themeService;
        LineCountViewModel = lineCountViewModel;
        _textEditorViewModel = textEditorViewModel;

        cursorPositionService.CursorPositionChanged += OnCursorPositionChanged;
        _themeService.ThemeChanged += OnThemeChanged;

        UpdateBrushes();
    }

    private void OnCursorPositionChanged(object? sender, CursorPositionChangedEventArgs e)
    {
        CursorPosition = e.Position;
        OnInvalidateRequired();
    }

    private void OnTextEditorSelectionChanged(object? sender, EventArgs e)
    {
        OnInvalidateRequired();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateBrushes();
    }

    private void UpdateBrushes()
    {
        // BackgroundBrush = GetResourceBrush("GutterBackground");
        // ForegroundBrush = GetResourceBrush("GutterDefault");
        // LineHighlightBrush = GetResourceBrush("GutterHighlight");
        // SelectedBrush = GetResourceBrush("GutterSelected");

        OnInvalidateRequired();
    }

    public event EventHandler InvalidateRequired;
    public event EventHandler WidthRecalculationRequired;
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnWidthRecalculationRequired()
    {
        WidthRecalculationRequired?.Invoke(this, EventArgs.Empty);
    }

    private object GetResourceBrush(string key)
    {
        return _themeService.GetResourceBrush(key) ?? GetTransparentBrush();
    }

    private object GetTransparentBrush()
    {
        return null;
    }

    public void Dispose()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        if (_textEditorViewModel.IsValueCreated)
            _textEditorViewModel.Value.SelectionChanged -= OnTextEditorSelectionChanged;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}