using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using meteor.App.Adapters;
using meteor.App.Rendering;
using meteor.App.Services;
using meteor.Core.Contexts;
using meteor.Core.Interfaces;
using meteor.Core.Models;
using meteor.Core.Models.Commands;
using meteor.Core.Models.Resources;
using meteor.Core.Services;
using meteor.Services;
using meteor.ViewModels;

namespace meteor.App.Views;

public partial class TextEditor : UserControl
{
    private ScrollableTextEditorViewModel _viewModel;
    private TextEditorContext _context;
    private RenderManager _renderManager;
    private InputManager _inputManager;

    public TextEditor()
    {
        Focusable = true;

        InitializeComponent();
        Initialize();
    }

    private void Initialize()
    {
        DataContextChanged += OnDataContextChanged;

        var textBuffer = new TextBuffer();
        var clipboardService = new AvaloniaClipboardService(this);
        var undoRedoManager = new UndoRedoManager<ITextBuffer>(textBuffer);
        var wordBoundaryService = new WordBoundaryService();

        var selectionHandler = new SelectionHandler(textBuffer, wordBoundaryService);
        var cursorManager = new CursorManager(textBuffer, selectionHandler, wordBoundaryService);

        var textEditorCommands = new TextEditorCommands(
            textBuffer,
            cursorManager,
            selectionHandler,
            clipboardService,
            undoRedoManager
        );

        var textEditorViewModel = new TextEditorViewModel(
            textBuffer,
            clipboardService,
            undoRedoManager,
            cursorManager,
            selectionHandler
        );

        var lineCountViewModel = new LineCountViewModel();

        var applicationResourceProvider = new ApplicationResourceProvider();
        var themeService = new ThemeService(applicationResourceProvider);
        var cursorPositionService = new CursorPositionService();
        var gutterViewModel = new GutterViewModel(
            cursorPositionService,
            lineCountViewModel,
            textEditorViewModel,
            themeService
        );

        _viewModel = new ScrollableTextEditorViewModel(textEditorViewModel, lineCountViewModel, gutterViewModel);
        DataContext = _viewModel;

        // Create TextEditorContext
        _context = new TextEditorContext(
            20,
            new BrushAdapter(new SolidColorBrush(Colors.White)),
            new BrushAdapter(new SolidColorBrush(Colors.LightGray)),
            new BrushAdapter(new SolidColorBrush(Colors.LightBlue)),
            new BrushAdapter(new SolidColorBrush(Colors.Black)),
            2,
            1,
            new FontFamilyAdapter(new FontFamily("Consolas")),
            13,
            new BrushAdapter(new SolidColorBrush(Colors.Black)),
            _viewModel,
            Core.Models.Rendering.FontStyle.Normal,
            Core.Models.Rendering.FontWeight.Normal,
            new BrushAdapter(new SolidColorBrush(Colors.Black))
        );

        var languageDefinitions = new Dictionary<string, ILanguageDefinition>();

        // Create RenderManager
        _renderManager = new RenderManager(
            _context,
            themeService,
            () => new SyntaxHighlighter(languageDefinitions)
        );

        // Create InputManager
        _inputManager = new InputManager(cursorManager, selectionHandler, textEditorCommands);

        _renderManager.AttachToViewModel(_viewModel);

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        KeyDown += OnKeyDown;
        TextInput += OnTextInput;
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.HorizontalOffset = ((ScrollViewer)sender).Offset.Y;
            _viewModel.VerticalOffset = ((ScrollViewer)sender).Offset.X;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredSize = new Size(
            _viewModel?.RequiredWidth ?? 0,
            _viewModel?.RequiredHeight ?? 0
        );

        Console.WriteLine($"Desired size: {desiredSize}");
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
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ScrollableTextEditorViewModel viewModel)
        {
            _viewModel = viewModel;
            if (_context != null) _context.ScrollableViewModel = _viewModel;
            if (_renderManager != null) _renderManager.AttachToViewModel(_viewModel);

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            InvalidateVisual();
            InvalidateMeasure();
        }
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        InvalidateVisual();
        InvalidateMeasure();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_renderManager != null && _viewModel?.TextEditorViewModel?.TextBuffer != null)
        {
            var drawingContext = new AvaloniaDrawingContext(context);
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
            _renderManager.Render(drawingContext);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        var position = e.GetPosition(this);
        position = position.WithY(position.Y + _viewModel.VerticalOffset)
            .WithX(position.X + _viewModel.HorizontalOffset);
        _inputManager.OnPointerPressed(new PointerPressedEventArgsAdapter(e));
        InvalidateVisual();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(this);
        position = position.WithY(position.Y + _viewModel.VerticalOffset)
            .WithX(position.X + _viewModel.HorizontalOffset);
        _inputManager.OnPointerMoved(new PointerEventArgsAdapter(e));
        InvalidateVisual();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var position = e.GetPosition(this);
        position = position.WithY(position.Y + _viewModel.VerticalOffset)
            .WithX(position.X + _viewModel.HorizontalOffset);
        _inputManager.OnPointerReleased(new PointerReleasedEventArgsAdapter(e));
        InvalidateVisual();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var handled = false;

        // Handle Ctrl key combinations
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
            // Handle other keys
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
        _renderManager.Dispose();

        PointerPressed -= OnPointerPressed;
        PointerMoved -= OnPointerMoved;
        PointerReleased -= OnPointerReleased;
        KeyDown -= OnKeyDown;
        TextInput -= OnTextInput;

        if (_viewModel != null) _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }
}