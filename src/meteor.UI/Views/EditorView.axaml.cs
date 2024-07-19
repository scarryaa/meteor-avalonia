using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Reactive;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Adapters;
using meteor.UI.Renderers;
using Vector = meteor.Core.Models.Vector;

namespace meteor.UI.Views;

public partial class EditorView : UserControl
{
    private readonly EditorRenderer _editorRenderer;
    private IEditorViewModel? _viewModel;
    private ScrollViewer? _scrollViewer;
    private Panel? _editorPanel;

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
        _editorPanel = this.FindControl<Panel>("EditorPanel");

        if (_scrollViewer != null && _viewModel != null)
            _scrollViewer.ScrollChanged += (sender, _) =>
            {
                if (sender is ScrollViewer scrollViewer)
                {
                    var offset = scrollViewer.Offset;
                    _viewModel.UpdateScrollOffset(new Vector(offset.X, offset.Y));
                }
            };

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
            _editorRenderer.UpdateTabService(_viewModel.TabService);
            _viewModel.PropertyChanged += (_, _e) =>
            {
                if (_e.PropertyName == nameof(IEditorViewModel.Text) ||
                    _e.PropertyName == nameof(IEditorViewModel.HighlightingResults) ||
                    _e.PropertyName == nameof(IEditorViewModel.Selection) ||
                    _e.PropertyName == nameof(IEditorViewModel.CursorPosition) ||
                    _e.PropertyName == nameof(IEditorViewModel.EditorWidth) ||
                    _e.PropertyName == nameof(IEditorViewModel.EditorHeight) ||
                    _e.PropertyName == nameof(IEditorViewModel.ScrollOffset))
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
        if (_viewModel == null || _editorPanel == null)
            return base.MeasureOverride(availableSize);

        _editorPanel.Width = Math.Max(_viewModel.EditorWidth, availableSize.Width);
        _editorPanel.Height = Math.Max(_viewModel.EditorHeight, availableSize.Height);

        return new Size(_editorPanel.Width, _editorPanel.Height);
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
        _editorRenderer.UpdateLineInfo();
        e.Handled = true;
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right ||
            e.Key == Key.Down || e.Key == Key.Up ||
            e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Enter ||
            (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
             (e.Key == Key.A || e.Key == Key.C || e.Key == Key.X || e.Key == Key.V)))
        {
            var meteorKey = KeyMapper.ToMeteorKey(e.Key);
            var meteorKeyModifiers = KeyMapper.ToMeteorKeyModifiers(e.KeyModifiers);
            var meteorKeyEventArgs = new Core.Models.Events.KeyEventArgs(meteorKey, meteorKeyModifiers);
            await _viewModel?.OnKeyDown(meteorKeyEventArgs);
        }

        _editorRenderer.UpdateLineInfo();
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_viewModel != null && _scrollViewer != null && _editorPanel != null)
        {
            var offset = _scrollViewer.Offset;
            var viewport = _scrollViewer.Viewport;

            // Adjust the clip rectangle to match the viewport
            var clipRect = new Rect(0, 0, viewport.Width, viewport.Height);

            using (context.PushClip(clipRect))
            {
                // Render the content
                _editorRenderer.Render(context, Bounds, _viewModel.HighlightingResults,
                    _viewModel.Selection, _viewModel.CursorPosition, offset.Y, offset.X);
            }
        }
    }
}