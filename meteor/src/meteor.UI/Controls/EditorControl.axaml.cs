using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.UI.Adapters;

namespace meteor.UI.Controls;

public partial class EditorControl : UserControl
{
    private readonly IEditorViewModel _viewModel;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;
    private readonly IScrollManager _scrollManager;
    private ScrollViewer _scrollViewer;
    private EditorContentControl _contentControl;
    private bool _isUpdatingFromScrollManager;

    public EditorControl(IEditorViewModel viewModel, ITextMeasurer textMeasurer, IEditorConfig config,
        IScrollManager scrollManager)
    {
        Focusable = true;
        _viewModel = viewModel;
        _textMeasurer = textMeasurer;
        _config = config;
        _scrollManager = scrollManager;

        SetupScrollViewer();
    }

    private void SetupScrollViewer()
    {
        _contentControl = new EditorContentControl(_viewModel, _textMeasurer, _config);
        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _contentControl
        };

        Content = _scrollViewer;

        _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        _scrollViewer.SizeChanged += ScrollViewer_SizeChanged;
        _scrollManager.ScrollChanged += ScrollManager_ScrollChanged;

        AttachedToVisualTree += (s, e) => InitializeScrollViewerSizes();
    }

    private void InitializeScrollViewerSizes()
    {
        _scrollManager.UpdateViewportAndExtentSizes(SizeAdapter.Convert(_scrollViewer.Viewport),
            SizeAdapter.Convert(_scrollViewer.Extent));
        _contentControl.Viewport = _scrollViewer.Viewport;
    }

    private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_isUpdatingFromScrollManager) return;

        _scrollManager.ScrollOffset = new Vector(_scrollViewer.Offset.X, _scrollViewer.Offset.Y);
        _contentControl.Offset = _scrollViewer.Offset;
    }

    private void ScrollViewer_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var newViewportSize = new Size(_scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height);
        var newExtentSize = new Size(_scrollViewer.Extent.Width, _scrollViewer.Extent.Height);

        _scrollManager.UpdateViewportAndExtentSizes(newViewportSize, newExtentSize);
        _contentControl.Viewport = new Avalonia.Size(newViewportSize.Width, newViewportSize.Height);
    }

    private void ScrollManager_ScrollChanged(object? sender, Vector e)
    {
        if (_isUpdatingFromScrollManager) return;
        
        _isUpdatingFromScrollManager = true;
        _scrollViewer.Offset = new Avalonia.Vector(e.X, e.Y);
        _isUpdatingFromScrollManager = false;
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
            _scrollManager.EnsureLineIsVisible(_viewModel.GetCursorLine(), _viewModel.GetCursorX());
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
