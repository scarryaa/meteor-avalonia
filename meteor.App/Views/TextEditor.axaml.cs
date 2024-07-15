using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using meteor.App.Rendering;
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
        InitializeComponent();
        Initialize();
    }

    private void Initialize()
    {
        Focusable = true;
        DataContextChanged += OnDataContextChanged;

        var textBuffer = new TextBuffer();
        var clipboardService = new ClipboardService();
        // TODO set the initial state to loaded content if needed
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
        var gutterViewModel = new GutterViewModel(
            null,
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
            14,
            new BrushAdapter(new SolidColorBrush(Colors.Black)),
            _viewModel
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

        // Attach ViewModel to InputManager and RenderManager
        _renderManager.AttachToViewModel(_viewModel);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ScrollableTextEditorViewModel viewModel)
        {
            _viewModel = viewModel;
            _context.ScrollableViewModel = _viewModel;
            _renderManager.AttachToViewModel(_viewModel);
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        if (_renderManager != null)
        {
            var drawingContext = new AvaloniaDrawingContext(context);
            _renderManager.Render(drawingContext);
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _inputManager.Dispose();
        _renderManager.Dispose();
    }
}