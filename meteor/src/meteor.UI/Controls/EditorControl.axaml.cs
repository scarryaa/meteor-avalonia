using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.UI.Adapters;
using Size = Avalonia.Size;

namespace meteor.UI.Controls;

public partial class EditorControl : UserControl
{
    private readonly IEditorViewModel _viewModel;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;
    private readonly IScrollManager _scrollManager;
    private ScrollViewer _scrollViewer;
    private EditorContentControl _contentControl;
    private GutterControl _gutterControl;
    private bool _isUpdatingFromScrollManager;

    public EditorControl(IEditorViewModel viewModel, ITextMeasurer textMeasurer, IEditorConfig config,
        IScrollManager scrollManager)
    {
        Focusable = true;
        _viewModel = viewModel;
        _textMeasurer = textMeasurer;
        _config = config;
        _scrollManager = scrollManager;

        SetupControls();
    }

    private void SetupControls()
    {
        _contentControl = new EditorContentControl(_viewModel, _textMeasurer, _config);
        _gutterControl = new GutterControl(_viewModel, _textMeasurer, _config);

        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _contentControl
        };

        var mainGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*")
        };

        Grid.SetColumn(_gutterControl, 0);
        Grid.SetColumn(_scrollViewer, 1);

        mainGrid.Children.Add(_gutterControl);
        mainGrid.Children.Add(_scrollViewer);

        Content = mainGrid;

        _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        _scrollViewer.SizeChanged += ScrollViewer_SizeChanged;
        _scrollManager.ScrollChanged += ScrollManager_ScrollChanged;

        AttachedToVisualTree += (s, e) =>
        {
            _scrollManager.GutterWidth = _gutterControl.DesiredSize.Width;
            InitializeScrollViewerSizes();
        };

        _contentControl.PointerPressed += ContentControl_PointerPressed;
        _contentControl.PointerMoved += ContentControl_PointerMoved;
        _contentControl.PointerReleased += ContentControl_PointerReleased;
    }

    private void ContentControl_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var documentPosition = GetDocumentPositionFromPoint(new Point(point.X, point.Y));
        UpdateCursorPosition(documentPosition, false);
        _viewModel.StartSelection(documentPosition);
        e.Handled = true;
    }

    private void ContentControl_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var point = e.GetPosition(this);
            var documentPosition = GetDocumentPositionFromPoint(new Point(point.X, point.Y));
            UpdateCursorPosition(documentPosition, true);
            _viewModel.UpdateSelection(documentPosition);
            e.Handled = true;
        }
    }

    private void ContentControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _viewModel.EndSelection();
        e.Handled = true;
    }

    private void UpdateCursorPosition(int documentPosition, bool isSelection)
    {
        _viewModel.SetCursorPosition(documentPosition);
        _scrollManager.EnsureLineIsVisible(_viewModel.GetCursorLine(), _viewModel.GetCursorX(), isSelection);
        _contentControl.InvalidateVisual();
    }

    private int GetDocumentPositionFromPoint(Point point)
    {
        var lineHeight = _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize) * 1.5;
        var adjustedY = point.Y + _scrollViewer.Offset.Y;
        var lineIndex = Math.Max(0, (int)Math.Floor(adjustedY / lineHeight));
        var lineStart = _viewModel.GetLineStartOffset(lineIndex);
        var clickX = point.X - _gutterControl.Viewport.Width + _scrollViewer.Offset.X;

        var line = _viewModel.GetContentSlice(lineIndex, lineIndex);
        var trimmedLine = line.TrimEnd('\r', '\n');

        // Measure the entire line, or assume a minimal width for an empty line
        var lineWidth = string.IsNullOrEmpty(trimmedLine)
            ? 0
            : _textMeasurer.MeasureText(trimmedLine, _config.FontFamily, _config.FontSize).Width;

        // If clickX is beyond the line width, return the end of the line
        if (clickX >= lineWidth) return lineStart + trimmedLine.Length;

        // If clickX is negative, return the start of the line
        if (clickX < 0) return lineStart;

        // Binary search for the closest character
        int left = 0, right = trimmedLine.Length;
        while (left < right)
        {
            var mid = (left + right) / 2;
            var width = _textMeasurer
                .MeasureText(trimmedLine.Substring(0, mid), _config.FontFamily, _config.FontSize).Width;
            if (width < clickX)
                left = mid + 1;
            else
                right = mid;
        }

        var charIndex = left;
        var documentPosition = lineStart + charIndex;

        return documentPosition;
    }
    
    private void InitializeScrollViewerSizes()
    {
        _scrollManager.UpdateViewportAndExtentSizes(SizeAdapter.Convert(_scrollViewer.Viewport),
            SizeAdapter.Convert(_scrollViewer.Extent));
        _contentControl.Viewport = _scrollViewer.Viewport;
        _gutterControl.Viewport = new Size(_gutterControl.Bounds.Width, _scrollViewer.Viewport.Height);
    }

    private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_isUpdatingFromScrollManager) return;

        _scrollManager.ScrollOffset = new Vector(_scrollViewer.Offset.X, _scrollViewer.Offset.Y);
        _contentControl.Offset = _scrollViewer.Offset;
        _gutterControl.UpdateScroll(new Avalonia.Vector(0, _scrollViewer.Offset.Y));
    }

    private void ScrollViewer_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var newViewportSize = new Core.Models.Size(_scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height);
        var newExtentSize = new Core.Models.Size(_scrollViewer.Extent.Width, _scrollViewer.Extent.Height);

        _scrollManager.UpdateViewportAndExtentSizes(newViewportSize, newExtentSize);
        _contentControl.Viewport = new Size(newViewportSize.Width, newViewportSize.Height);
        _gutterControl.Viewport = new Size(_gutterControl.Bounds.Width, newViewportSize.Height);
        _contentControl.InvalidateVisual();
    }

    private void ScrollManager_ScrollChanged(object? sender, Vector e)
    {
        if (_isUpdatingFromScrollManager) return;
            
        _isUpdatingFromScrollManager = true;
        _scrollViewer.Offset = new Avalonia.Vector(e.X, e.Y);
        _isUpdatingFromScrollManager = false;
        _contentControl.InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var isModifierOrPageKey = e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                                  e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                                  e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                                  e.Key == Key.LWin || e.Key == Key.RWin ||
                                  e.Key == Key.PageUp || e.Key == Key.PageDown ||
                                  e.KeyModifiers.HasFlag(KeyModifiers.Alt) ||
                                  e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                                  e.KeyModifiers.HasFlag(KeyModifiers.Meta) ||
                                  e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.PageUp:
                _scrollManager.PageUp();
                e.Handled = true;
                break;
            case Key.PageDown:
                _scrollManager.PageDown();
                e.Handled = true;
                break;
            default:
                _viewModel.HandleKeyDown(KeyEventArgsAdapter.Convert(e));
                break;
        }

        _contentControl.InvalidateVisual();
        _contentControl.InvalidateMeasure();


        if (!isModifierOrPageKey)
            _scrollManager.EnsureLineIsVisible(_viewModel.GetCursorLine(), _viewModel.GetCursorX(),
                _viewModel.HasSelection());
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _viewModel.HandleTextInput(TextInputEventArgsAdapter.Convert(e));
        _contentControl.InvalidateVisual();
        _contentControl.InvalidateMeasure();
        _scrollManager.EnsureLineIsVisible(_viewModel.GetCursorLine(), _viewModel.GetCursorX());
    }
}
