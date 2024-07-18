using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Reactive;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Adapters;
using meteor.UI.Renderers;

namespace meteor.UI.Views;

public partial class EditorView : UserControl
{
    private readonly EditorRenderer _editorRenderer;
    private IEditorViewModel? _viewModel;
    private ScrollViewer? _scrollViewer;

    public EditorView()
    {
        InitializeComponent();
        _editorRenderer = new EditorRenderer(InvalidateVisual);
        DataContextChanged += OnDataContextChanged; 
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _scrollViewer = this.FindControl<ScrollViewer>("ScrollViewer");

        if (_scrollViewer != null) _scrollViewer.ScrollChanged += (_, _) => InvalidateVisual();

        PointerPressed += OnEditorPointerPressed;
        PointerMoved += OnEditorPointerMoved;
        PointerReleased += OnEditorPointerReleased;

        this.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>(bounds =>
        {
            if (_viewModel != null) _viewModel.UpdateWindowSize(bounds.Width, bounds.Height);
        }));
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as IEditorViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (_, _e) =>
            {
                if (_e.PropertyName == nameof(IEditorViewModel.Text) ||
                    _e.PropertyName == nameof(IEditorViewModel.HighlightingResults) ||
                    _e.PropertyName == nameof(IEditorViewModel.Selection) ||
                    _e.PropertyName == nameof(IEditorViewModel.CursorPosition))
                {
                    InvalidateMeasure();
                    InvalidateVisual();
                }
            };
            InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_viewModel == null) return base.MeasureOverride(availableSize);

        var desiredSize = _viewModel.CalculateEditorSize(availableSize.Width, availableSize.Height);
        return new Size(Math.Max(desiredSize.width, availableSize.Width),
            Math.Max(desiredSize.height, availableSize.Height));
    }

    private void OnEditorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var position = e.GetPosition(_scrollViewer);
        var adaptedArgs = EventArgsAdapters.ToPointerPressedEventArgs(e, this);

        var updatedArgs = new Core.Models.Events.PointerPressedEventArgs(
            adaptedArgs.Index,
            position.X,
            position.Y,
            adaptedArgs.Modifiers,
            adaptedArgs.IsLeftButtonPressed,
            adaptedArgs.IsRightButtonPressed,
            adaptedArgs.IsMiddleButtonPressed
        );

        _viewModel?.OnPointerPressed(updatedArgs);
    }

    private void OnEditorPointerMoved(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(_scrollViewer);
        var adaptedArgs = EventArgsAdapters.ToPointerEventArgsModel(e, this);

        var updatedArgs = new Core.Models.Events.PointerEventArgs(
            index: adaptedArgs.Index,
            x: position.X,
            y: position.Y,
            modifiers: adaptedArgs.Modifiers,
            isLeftButtonPressed: adaptedArgs.IsLeftButtonPressed,
            isRightButtonPressed: adaptedArgs.IsRightButtonPressed,
            isMiddleButtonPressed: adaptedArgs.IsMiddleButtonPressed
        );

        _viewModel?.OnPointerMoved(updatedArgs);
    }

    private void OnEditorPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var position = e.GetPosition(_scrollViewer);
        var adaptedArgs = EventArgsAdapters.ToPointerReleasedEventArgs(e, this);

        var updatedArgs = new Core.Models.Events.PointerReleasedEventArgs(
            adaptedArgs.Index,
            position.X,
            position.Y,
            adaptedArgs.Modifiers,
            adaptedArgs.IsLeftButtonPressed,
            adaptedArgs.IsRightButtonPressed,
            adaptedArgs.IsMiddleButtonPressed
        );

        _viewModel?.OnPointerReleased(updatedArgs);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _viewModel?.OnTextInput(EventArgsAdapters.ToTextInputEventArgsModel(e));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right ||
            e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Enter)
        {
            var meteorKey = KeyMapper.ToMeteorKey(e.Key);
            var meteorKeyEventArgs = new Core.Models.Events.KeyEventArgs(meteorKey);
            _viewModel?.OnKeyDown(meteorKeyEventArgs);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_viewModel != null && _scrollViewer != null)
        {
            var offset = _scrollViewer.Offset;
            var viewport = _scrollViewer.Viewport;
            var (firstVisibleLine, visibleLineCount) = _editorRenderer.CalculateVisibleLines(viewport.Height, offset.Y);

            var clipRect = new Rect(offset.X, offset.Y, viewport.Width, viewport.Height);
            var renderRect = new Rect(0, 0, Bounds.Width, Bounds.Height);

            using (context.PushClip(clipRect))
            {
                context.DrawRectangle(Brushes.White, null, renderRect);
                _editorRenderer.Render(context, renderRect, _viewModel.Text, _viewModel.HighlightingResults,
                    _viewModel.Selection, _viewModel.CursorPosition, firstVisibleLine, visibleLineCount,
                    -offset.X, -offset.Y);
            }
        }
    }
}