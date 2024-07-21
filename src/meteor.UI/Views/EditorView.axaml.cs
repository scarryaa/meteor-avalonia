using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Reactive;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Config;
using meteor.UI.Adapters;
using meteor.UI.Interfaces;
using meteor.UI.Renderers;
using Microsoft.Extensions.DependencyInjection;
using Vector = meteor.Core.Models.Vector;

namespace meteor.UI.Views;

public partial class EditorView : UserControl
{
    private readonly EditorRenderer _editorRenderer;
    private Panel? _editorPanel;
    private ScrollViewer? _scrollViewer;
    private IEditorViewModel? _oldViewModel;
    private IEditorViewModel? _viewModel;
    private GutterView? _gutterView;
    private double _gutterWidth;

    public EditorView()
    {
        InitializeComponent();

        if (Application.Current is App app)
        {
            var serviceProvider = app.ServiceProvider;
            var themeManager = serviceProvider.GetRequiredService<IThemeManager>();
            var themeConfig = serviceProvider.GetRequiredService<ThemeConfig>();
            _editorRenderer = new EditorRenderer(InvalidateVisual, themeManager, themeConfig);
        }
        else
        {
            throw new InvalidOperationException("Unable to access the application's service provider.");
        }

        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _scrollViewer = this.FindControl<ScrollViewer>("ScrollViewer");
        _editorPanel = this.FindControl<Panel>("EditorPanel");
        _gutterView = this.FindControl<GutterView>("GutterView");

        if (_scrollViewer != null && _viewModel != null)
        {
            _scrollViewer.ScrollChanged += (sender, e) =>
            {
                if (sender is ScrollViewer scrollViewer)
                {
                    var offset = scrollViewer.Offset;
                    _viewModel.UpdateScrollOffset(new Vector(offset.X, offset.Y));
                }
            };

            _scrollViewer.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == nameof(ScrollViewer.Viewport) || e.Property.Name == nameof(ScrollViewer.Extent))
                    UpdateEditorSize();
            };
        }
        
        PointerPressed += OnEditorPointerPressed;
        PointerMoved += OnEditorPointerMoved;
        PointerReleased += OnEditorPointerReleased;

        this.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>(bounds =>
        {
            UpdateEditorSize();
        }));

        if (_gutterView != null)
            _gutterView.PropertyChanged += (_, __) =>
            {
                _gutterWidth = _gutterView.Bounds.Width;
                InvalidateMeasure();
                InvalidateVisual();
            };
    }


    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old view model events
        if (_oldViewModel != null)
        {
            _oldViewModel.InvalidateMeasureRequested -= OnInvalidateMeasureRequested;
            _oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as IEditorViewModel;

        if (_viewModel != null)
        {
            _editorRenderer.UpdateTabService(_viewModel.TabService);

            _viewModel.InvalidateMeasureRequested += OnInvalidateMeasureRequested;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            InvalidateMeasure();
        }

        _oldViewModel = _viewModel;
    }

    private void OnInvalidateMeasureRequested(object? sender, EventArgs e)
    {
        InvalidateMeasure();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IEditorViewModel.Text) or nameof(IEditorViewModel.HighlightingResults)
            or nameof(IEditorViewModel.Selection) or nameof(IEditorViewModel.CursorPosition)
            or nameof(IEditorViewModel.EditorWidth) or nameof(IEditorViewModel.EditorHeight)
            or nameof(IEditorViewModel.ScrollOffset))
        {
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_viewModel == null || _editorPanel == null || _gutterView == null)
            return base.MeasureOverride(availableSize);

        _editorPanel.Width = Math.Max(_viewModel.EditorWidth, availableSize.Width - _gutterWidth);
        _editorPanel.Height = Math.Max(_viewModel.EditorHeight, availableSize.Height);

        return new Size(_editorPanel.Width + _gutterWidth, _editorPanel.Height);
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

        if ((e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right ||
             e.Key == Key.Down || e.Key == Key.Up ||
             e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Enter ||
             (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
              e.Key is Key.A or Key.C or Key.X or Key.V)) && _viewModel != null)
        {
            var meteorKey = KeyMapper.ToMeteorKey(e.Key);
            var meteorKeyModifiers = KeyMapper.ToMeteorKeyModifiers(e.KeyModifiers);
            var meteorKeyEventArgs = new Core.Models.Events.KeyEventArgs(meteorKey, meteorKeyModifiers);
            await _viewModel.OnKeyDown(meteorKeyEventArgs);
        }

        _editorRenderer.UpdateLineInfo();
    }


    private void UpdateEditorSize()
    {
        if (_viewModel != null && _scrollViewer != null)
        {
            var viewportHeight = _scrollViewer.Viewport.Height;
            var viewportWidth = _scrollViewer.Viewport.Width;
            var extentHeight = _scrollViewer.Extent.Height;
            _viewModel.UpdateEditorSize(_viewModel.EditorWidth, extentHeight, viewportHeight, viewportWidth);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_viewModel != null && _scrollViewer != null && _editorPanel != null)
        {
            var offset = _scrollViewer.Offset;
            var viewport = _scrollViewer.Viewport;

            var clipRect = new Rect(_gutterWidth, 0, viewport.Width + _gutterWidth + 1, viewport.Height);

            using (context.PushClip(clipRect))
            {
                // Render the content
                _editorRenderer.Render(context, new Rect(_gutterWidth, 0, Bounds.Width - _gutterWidth, Bounds.Height),
                    _viewModel.HighlightingResults,
                    _viewModel.Selection, _viewModel.CursorPosition, offset.Y, offset.X - _gutterWidth);
            }
        }
    }
}