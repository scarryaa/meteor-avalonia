using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using meteor.App.Adapters;
using meteor.App.Rendering;
using meteor.Core.Contexts;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Services;
using meteor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace meteor.App.Views;

public partial class TextEditor : UserControl
{
    private readonly ITextEditorViewModel? _viewModel;
    private readonly InputManager _inputManager;
    private readonly ILogger<TextEditor> _logger;

    public RenderManager RenderManager { get; }
    public ScrollViewer? ScrollViewer { get; private set; }

    public TextEditor()
    {
        Focusable = true;

        var services = App.Current?.Services;
        if (services == null) throw new InvalidOperationException("Services not available");

        _logger = services.GetRequiredService<ILogger<TextEditor>>();
        _viewModel = services.GetRequiredService<ITextEditorViewModel>();
        _inputManager = services.GetRequiredService<InputManager>();
        RenderManager = services.GetRequiredService<RenderManager>();

        DataContext = _viewModel;

        InitializeComponent(services);
        SetupEventHandlers();

        RenderManager.AttachToViewModel(_viewModel);
    }

    private void InitializeComponent(IServiceProvider services)
    {
        var textContentLogger = services.GetRequiredService<ILogger<TextEditorContent>>();
        var content = new TextEditorContent(this, textContentLogger);

        ScrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };

        Content = ScrollViewer;
    }

    private TextEditorContext CreateTextEditorContext()
    {
        if (_viewModel != null)
            return new TextEditorContext(
                _viewModel.LineHeight,
                new BrushAdapter(new SolidColorBrush(Colors.White)),
                new BrushAdapter(new SolidColorBrush(Colors.LightGray)),
                new BrushAdapter(new SolidColorBrush(Colors.LightBlue)),
                new BrushAdapter(new SolidColorBrush(Colors.Black)),
                0,
                1,
                new FontFamilyAdapter(new FontFamily(_viewModel.FontFamily)),
                _viewModel.FontSize,
                new BrushAdapter(new SolidColorBrush(Colors.Black)),
                _viewModel,
                Core.Models.Rendering.FontStyle.Normal,
                Core.Models.Rendering.FontWeight.Normal,
                new BrushAdapter(new SolidColorBrush(Colors.Black))
            );

        throw new InvalidOperationException("ViewModel not available");
    }

    private void SetupEventHandlers()
    {
        if (ScrollViewer == null || _viewModel == null) return;

        ScrollViewer.ScrollChanged += OnScrollChanged;
        ScrollViewer.LayoutUpdated += OnLayoutUpdated;

        if (ScrollViewer.Content is TextEditorContent editorContent)
        {
            editorContent.PointerPressed += OnPointerPressed;
            editorContent.PointerMoved += OnPointerMoved;
            editorContent.PointerReleased += OnPointerReleased;
        }

        KeyDown += OnKeyDown;
        TextInput += OnTextInput;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _viewModel.InvalidateRequired += (_, _) => InvalidateVisual();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        UpdateViewportSize();
        InvalidateVisual();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateViewportAndInvalidate();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        UpdateViewportAndInvalidate();
    }

    private void UpdateViewportAndInvalidate()
    {
        UpdateViewportSize();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_viewModel == null) return new Size(0, 0);

        var width = Math.Max(_viewModel.RequiredWidth, availableSize.Width);
        var height = Math.Max(_viewModel.RequiredHeight, availableSize.Height);

        var desiredSize = new Size(width, height);
        _logger.LogDebug($"TextEditor Desired size: {desiredSize.Width}, {desiredSize.Height}");
        return desiredSize;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateBounds();
        this.GetObservable(BoundsProperty).Subscribe(_ => UpdateBounds());
    }

    private void UpdateBounds()
    {
        if (_viewModel != null)
        {
            _viewModel.WindowWidth = Bounds.Width;
            _viewModel.WindowHeight = Bounds.Height;
        }

        UpdateViewportSize();
    }

    private void UpdateViewportSize()
    {
        if (ScrollViewer == null || _viewModel == null) return;

        _viewModel.ViewportWidth = ScrollViewer.Viewport.Width;
        _viewModel.ViewportHeight = ScrollViewer.Viewport.Height;
        _logger.LogDebug($"Updated viewport size: {_viewModel.ViewportWidth}x{_viewModel.ViewportHeight}");
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ITextEditorViewModel.RequiredWidth):
            case nameof(ITextEditorViewModel.RequiredHeight):
                InvalidateMeasure();
                (ScrollViewer?.Content as TextEditorContent)?.UpdateSize();
                break;
            case nameof(ITextEditorViewModel.FontFamily):
            case nameof(ITextEditorViewModel.FontSize):
                UpdateTextEditorContext();
                break;
        }

        InvalidateVisual();
    }

    private void UpdateTextEditorContext()
    {
        var newContext = CreateTextEditorContext();
        RenderManager.UpdateContext(newContext);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        var position = GetAdjustedPosition(e);
        _inputManager.OnPointerPressed(new PointerPressedEventArgsAdapter(e));

        _logger.LogDebug($"Mouse down: {position}");
        if (_viewModel != null)
            _logger.LogDebug($"Selection started: {_viewModel.SelectionStart}, {_viewModel.SelectionEnd}");
        InvalidateVisual();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        _inputManager.OnPointerMoved(new PointerEventArgsAdapter(e));
        InvalidateVisual();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var position = GetAdjustedPosition(e);
        _inputManager.OnPointerReleased(new PointerReleasedEventArgsAdapter(e));

        _logger.LogDebug($"Mouse up: {position}");
        if (_viewModel != null)
            _logger.LogDebug($"Selection ended: {_viewModel.SelectionStart}, {_viewModel.SelectionEnd}");
        InvalidateVisual();
    }

    private Point GetAdjustedPosition(PointerEventArgs e)
    {
        var position = e.GetPosition(ScrollViewer?.Content as Visual);
        if (ScrollViewer != null)
            return new Point(position.X + ScrollViewer.Offset.X, position.Y + ScrollViewer.Offset.Y);

        throw new InvalidOperationException("ScrollViewer not available");
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (HandleKeyDown(e))
        {
            e.Handled = true;
            InvalidateVisual();
            InvalidateMeasure();
        }
    }

    private bool HandleKeyDown(KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return HandleControlKeyDown(e.Key);
        return HandleRegularKeyDown(e.Key);
    }

    private bool HandleControlKeyDown(Key key)
    {
        switch (key)
        {
            case Key.C:
            case Key.V:
            case Key.X:
            case Key.A:
            case Key.Z:
                _ = _inputManager.OnKeyDown(new KeyEventArgsAdapter(new KeyEventArgs
                    { Key = key, KeyModifiers = KeyModifiers.Control }));
                return true;
            default:
                return false;
        }
    }

    private bool HandleRegularKeyDown(Key key)
    {
        switch (key)
        {
            case Key.Left:
            case Key.Right:
            case Key.Up:
            case Key.Down:
            case Key.Home:
            case Key.End:
            case Key.Back:
            case Key.Delete:
            case Key.Enter:
                _ = _inputManager.OnKeyDown(new KeyEventArgsAdapter(new KeyEventArgs { Key = key }));
                return true;
            default:
                return false;
        }
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        _inputManager.OnTextInput(new TextInputEventArgsAdapter(e));
        InvalidateVisual();
        InvalidateMeasure();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _inputManager.Dispose();

        RemoveEventHandlers();
    }

    private void RemoveEventHandlers()
    {
        if (ScrollViewer?.Content is TextEditorContent editorContent)
        {
            editorContent.PointerPressed -= OnPointerPressed;
            editorContent.PointerMoved -= OnPointerMoved;
            editorContent.PointerReleased -= OnPointerReleased;
        }

        KeyDown -= OnKeyDown;
        TextInput -= OnTextInput;

        if (_viewModel != null) _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

        if (ScrollViewer != null)
        {
            ScrollViewer.ScrollChanged -= OnScrollChanged;
            ScrollViewer.LayoutUpdated -= OnLayoutUpdated;
        }
    }
}