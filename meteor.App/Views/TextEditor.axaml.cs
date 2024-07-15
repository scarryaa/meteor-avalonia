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
    private ITextEditorViewModel _viewModel;
    private TextEditorContext _context;
    private InputManager _inputManager;
    private ILogger<TextEditor> _logger;
    
    public RenderManager RenderManager { get; private set; }

    public ScrollViewer ScrollViewer { get; private set; }

    public TextEditor()
    {
        Focusable = true;
        InitializeComponent();
        Initialize();
    }

    private void InitializeComponent()
    {
        _logger = App.Current.Services.GetRequiredService<ILogger<TextEditor>>();
        var textContentLogger = App.Current.Services.GetRequiredService<ILogger<TextEditorContent>>();

        var content = new TextEditorContent(this, textContentLogger);

        ScrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };

        Content = ScrollViewer;
    }

    private void Initialize()
    {
        var services = App.Current.Services;
        if (services == null) return;

        _viewModel = services.GetRequiredService<ITextEditorViewModel>();
        DataContext = _viewModel;

        _context = new TextEditorContext(
            _viewModel.LineHeight,
            new BrushAdapter(new SolidColorBrush(Colors.White)),
            new BrushAdapter(new SolidColorBrush(Colors.LightGray)),
            new BrushAdapter(new SolidColorBrush(Colors.LightBlue)),
            new BrushAdapter(new SolidColorBrush(Colors.Black)),
            0,
            1,
            new FontFamilyAdapter(new FontFamily("Consolas")),
            13,
            new BrushAdapter(new SolidColorBrush(Colors.Black)),
            _viewModel,
            Core.Models.Rendering.FontStyle.Normal,
            Core.Models.Rendering.FontWeight.Normal,
            new BrushAdapter(new SolidColorBrush(Colors.Black))
        );

        _inputManager = services.GetRequiredService<InputManager>();
        RenderManager = services.GetRequiredService<RenderManager>();
        RenderManager.AttachToViewModel(_viewModel);

        ScrollViewer.ScrollChanged += OnScrollChanged;
        ScrollViewer.LayoutUpdated += OnLayoutUpdated;

        var editorContent = ScrollViewer.Content as TextEditorContent;
        if (editorContent != null)
        {
            editorContent.PointerPressed += OnPointerPressed;
            editorContent.PointerMoved += OnPointerMoved;
            editorContent.PointerReleased += OnPointerReleased;
        }

        KeyDown += OnKeyDown;
        TextInput += OnTextInput;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateViewportSize();
        InvalidateVisual();
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        UpdateViewportSize();
        InvalidateVisual();
    }

    private void OnLayoutUpdated(object sender, EventArgs e)
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
            UpdateViewportSize();
        }
    }

    private void UpdateViewportSize()
    {
        if (ScrollViewer != null && _viewModel != null)
        {
            _viewModel.ViewportWidth = ScrollViewer.Viewport.Width;
            _viewModel.ViewportHeight = ScrollViewer.Viewport.Height;
            _logger.LogDebug($"Updated viewport size: {_viewModel.ViewportWidth}x{_viewModel.ViewportHeight}");
        }
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ITextEditorViewModel.RequiredWidth) ||
            e.PropertyName == nameof(ITextEditorViewModel.RequiredHeight))
        {
            InvalidateMeasure();
            (ScrollViewer.Content as TextEditorContent)?.UpdateSize();
        }
        InvalidateVisual();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        Focus();
        var position = e.GetPosition(ScrollViewer.Content as Visual);
        position = position.WithY(position.Y + ScrollViewer.Offset.Y)
            .WithX(position.X + ScrollViewer.Offset.X);
        _inputManager.OnPointerPressed(new PointerPressedEventArgsAdapter(e));
        InvalidateVisual();
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        var position = e.GetPosition(ScrollViewer.Content as Visual);
        position = position.WithY(position.Y + ScrollViewer.Offset.Y)
            .WithX(position.X + ScrollViewer.Offset.X);
        _inputManager.OnPointerMoved(new PointerEventArgsAdapter(e));
        InvalidateVisual();
    }

    private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        var position = e.GetPosition(ScrollViewer.Content as Visual);
        position = position.WithY(position.Y + ScrollViewer.Offset.Y)
            .WithX(position.X + ScrollViewer.Offset.X);
        _inputManager.OnPointerReleased(new PointerReleasedEventArgsAdapter(e));
        InvalidateVisual();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        var handled = false;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            switch (e.Key)
            {
                case Key.C:
                case Key.V:
                case Key.X:
                case Key.A:
                case Key.Z:
                    _ = _inputManager.OnKeyDown(new KeyEventArgsAdapter(e));
                    handled = true;
                    break;
            }
        else
            switch (e.Key)
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
                    _ = _inputManager.OnKeyDown(new KeyEventArgsAdapter(e));
                    handled = true;
                    break;
            }

        if (handled) e.Handled = true;

        InvalidateVisual();
        InvalidateMeasure();
    }

    private void OnTextInput(object sender, TextInputEventArgs e)
    {
        _inputManager.OnTextInput(new TextInputEventArgsAdapter(e));
        InvalidateVisual();
        InvalidateMeasure();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _inputManager.Dispose();
        RenderManager.Dispose();

        var editorContent = ScrollViewer.Content as TextEditorContent;
        if (editorContent != null)
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