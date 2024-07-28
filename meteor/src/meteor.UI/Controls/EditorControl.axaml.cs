using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Adapters;
using meteor.UI.Interfaces.Services.Editor;
using Point = meteor.Core.Models.Point;

namespace meteor.UI.Controls;

public partial class EditorControl : UserControl
{
    private readonly IEditorViewModel _viewModel;
    private readonly IScrollManager? _scrollManager;
    private readonly IEditorLayoutManager _layoutManager;
    private readonly IEditorInputHandler _inputHandler;
    private readonly IPointerEventHandler _pointerEventHandler;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;

    private ScrollViewer? _scrollViewer;
    private EditorContentControl? _contentControl;
    private GutterControl? _gutterControl;

    public EditorControl(IEditorViewModel viewModel, IScrollManager scrollManager,
        IEditorLayoutManager layoutManager, IEditorInputHandler inputHandler,
        IPointerEventHandler pointerEventHandler, ITextMeasurer textMeasurer, IEditorConfig config)
    {
        Focusable = true;
        _viewModel = viewModel;
        _scrollManager = scrollManager;
        _layoutManager = layoutManager;
        _inputHandler = inputHandler;
        _pointerEventHandler = pointerEventHandler;
        _textMeasurer = textMeasurer;
        _config = config;

        InitializeComponent();
        SetupEventHandlers();
    }

    private void InitializeComponent()
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
    }

    private void SetupEventHandlers()
    {
        if (_scrollViewer is null || _scrollManager is null || _contentControl is null || _gutterControl is null)
        {
            Console.WriteLine(
                "ScrollViewer, ScrollManager, ContentControl, or GutterControl is null. Cannot setup event handlers.");
            return;
        }
        
        _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        _scrollViewer.SizeChanged += ScrollViewer_SizeChanged;
        _scrollManager.ScrollChanged += ScrollManager_ScrollChanged;

        AttachedToVisualTree += (_, _) =>
        {
            _layoutManager.InitializeLayout(_scrollViewer, _contentControl, _gutterControl);
        };

        _contentControl.PointerPressed += ContentControl_PointerPressed;
        _contentControl.PointerMoved += ContentControl_PointerMoved;
        _contentControl.PointerReleased += ContentControl_PointerReleased;

        _gutterControl.LineSelected += GutterControl_LineSelected;
        _gutterControl.ScrollRequested += GutterControl_ScrollRequested;
    }

    private void GutterControl_ScrollRequested(object? sender, Vector e)
    {
        if (_scrollViewer != null)
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, _scrollViewer.Offset.Y + e.Y);
    }

    private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        _layoutManager.HandleScrollChanged(_scrollViewer, _contentControl, _gutterControl);
        if (_gutterControl != null)
            _gutterControl.UpdateScroll(new Vector(_scrollViewer.Offset.X, _scrollViewer.Offset.Y));
    }

    private void ScrollViewer_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        _layoutManager.HandleSizeChanged(_scrollViewer, _contentControl, _gutterControl);
        if (_gutterControl != null) _gutterControl.Viewport = e.NewSize;
    }

    private void ScrollManager_ScrollChanged(object? sender, Core.Models.Vector e)
    {
        _layoutManager.HandleScrollManagerChanged(_scrollViewer, _contentControl, e);
    }

    private void ContentControl_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(_scrollViewer);
        _pointerEventHandler.HandlePointerPressed(_viewModel, new Point(point.X, point.Y));
        e.Handled = true;
    }

    private void ContentControl_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(_scrollViewer).Properties.IsLeftButtonPressed)
        {
            var point = e.GetPosition(_scrollViewer);
            _pointerEventHandler.HandlePointerMoved(_viewModel, new Point(point.X, point.Y));
            e.Handled = true;
        }
    }

    private void ContentControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pointerEventHandler.HandlePointerReleased(_viewModel);
        e.Handled = true;
    }

    private void GutterControl_LineSelected(object? sender, int lineIndex)
    {
        var lineStartOffset = _viewModel.GetLineStartOffset(lineIndex);
        var nextLineStartOffset = _viewModel.GetLineStartOffset(lineIndex + 1);

        _viewModel.SetCursorPosition(lineStartOffset);
        _viewModel.StartSelection(lineStartOffset);
        _viewModel.UpdateSelection(nextLineStartOffset - 1);

        // Ensure the selected line is visible
        if (_scrollViewer != null)
        {
            var lineY = lineIndex * _scrollManager.LineHeight;
            var currentScrollY = _scrollViewer.Offset.Y;
            var viewportHeight = _scrollViewer.Viewport.Height;

            if (lineY < currentScrollY || lineY > currentScrollY + viewportHeight)
                _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, lineY);
        }

        _contentControl?.InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _inputHandler.HandleKeyDown(_viewModel, new KeyDownEventArgsAdapter(e));
        _contentControl?.InvalidateVisual();
        _contentControl?.InvalidateMeasure();
        _gutterControl?.InvalidateVisual();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _inputHandler.HandleTextInput(_viewModel, new TextInputEventArgsAdapter(e));
        _contentControl?.InvalidateVisual();
        _contentControl?.InvalidateMeasure();
        _gutterControl?.InvalidateVisual();
    }
}