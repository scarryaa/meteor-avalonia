using System.ComponentModel;
using System.Runtime.CompilerServices;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Events;

namespace meteor.ViewModels;

public class GutterViewModel : IGutterViewModel
{
    private readonly IThemeService _themeService;
    private readonly ITextEditorViewModel _textEditorViewModel;
    private double _lineHeight;
    private IScrollManager _scrollManager;

    public ILineCountViewModel LineCountViewModel { get; }
    public int CursorPosition { get; set; }
    public string FontFamily { get; set; }
    public double FontSize { get; set; }

    public double LineHeight
    {
        get => _lineHeight;
        set => SetProperty(ref _lineHeight, value);
    }

    public object BackgroundBrush { get; set; }
    public object ForegroundBrush { get; set; }
    public object LineHighlightBrush { get; set; }
    public object SelectedBrush { get; set; }

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
        ITextEditorViewModel textEditorViewModel,
        IThemeService themeService)
    {
        _themeService = themeService;
        LineCountViewModel = lineCountViewModel;
        _textEditorViewModel = textEditorViewModel;

        cursorPositionService.CursorPositionChanged += OnCursorPositionChanged;
        _textEditorViewModel.SelectionChanged += OnTextEditorSelectionChanged;
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
        BackgroundBrush = GetResourceBrush("GutterBackground");
        ForegroundBrush = GetResourceBrush("GutterDefault");
        LineHighlightBrush = GetResourceBrush("GutterHighlight");
        SelectedBrush = GetResourceBrush("GutterSelected");

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
        _textEditorViewModel.SelectionChanged -= OnTextEditorSelectionChanged;
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