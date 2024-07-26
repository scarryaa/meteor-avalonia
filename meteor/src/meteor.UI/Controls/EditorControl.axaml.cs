using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.UI.Adapters;
using meteor.UI.Interfaces.Services.Editor;

namespace meteor.UI.Controls;

public partial class EditorControl : UserControl
{
    private readonly IEditorViewModel _viewModel;
    private readonly IScrollManager _scrollManager;
    private readonly IEditorLayoutManager _layoutManager;
    private readonly IEditorInputHandler _inputHandler;
    private readonly IPointerEventHandler _pointerEventHandler;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;

    private ScrollViewer _scrollViewer;
    private EditorContentControl _contentControl;
    private GutterControl _gutterControl;

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
        _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        _scrollViewer.SizeChanged += ScrollViewer_SizeChanged;
        _scrollManager.ScrollChanged += ScrollManager_ScrollChanged;

        AttachedToVisualTree += (s, e) =>
        {
            _layoutManager.InitializeLayout(_scrollViewer, _contentControl, _gutterControl);
        };

        _contentControl.PointerPressed += ContentControl_PointerPressed;
        _contentControl.PointerMoved += ContentControl_PointerMoved;
        _contentControl.PointerReleased += ContentControl_PointerReleased;
    }

    private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        _layoutManager.HandleScrollChanged(_scrollViewer, _contentControl, _gutterControl);
    }

    private void ScrollViewer_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        _layoutManager.HandleSizeChanged(_scrollViewer, _contentControl, _gutterControl);
    }

    private void ScrollManager_ScrollChanged(object? sender, Vector e)
    {
        _layoutManager.HandleScrollManagerChanged(_scrollViewer, _contentControl, e);
    }

    private void ContentControl_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(_scrollViewer);
        _pointerEventHandler.HandlePointerPressed(new Point(point.X, point.Y));
        e.Handled = true;
    }

    private void ContentControl_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(_scrollViewer).Properties.IsLeftButtonPressed)
        {
            var point = e.GetPosition(_scrollViewer);
            _pointerEventHandler.HandlePointerMoved(new Point(point.X, point.Y));
            e.Handled = true;
        }
    }

    private void ContentControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pointerEventHandler.HandlePointerReleased();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _inputHandler.HandleKeyDown(new KeyDownEventArgsAdapter(e));
        _contentControl.InvalidateVisual();
        _contentControl.InvalidateMeasure();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _inputHandler.HandleTextInput(new TextInputEventArgsAdapter(e));
        _contentControl.InvalidateVisual();
        _contentControl.InvalidateMeasure();
    }
}