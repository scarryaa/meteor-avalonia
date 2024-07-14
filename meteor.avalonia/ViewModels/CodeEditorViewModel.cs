using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using meteor.avalonia.Services;
using meteor.core.Interfaces;
using meteor.core.Models;
using meteor.core.Services;
using meteor.languageserver;
using meteor.rendering.Services;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using ReactiveUI;

namespace meteor.avalonia.ViewModels;

public class CodeEditorViewModel : ViewModelBase
{
    private readonly TextBuffer _textBuffer;
    private readonly LanguageServerClient _languageServerClient;
    private readonly UndoRedoManager _undoRedoManager;
    private readonly IClipboard _clipboardService;
    private readonly SelectionManager _selectionManager;
    private readonly CursorManager _cursorManager;
    private readonly InputManager _inputManager;
    private double _canvasWidth;
    private double _canvasHeight;

    public CodeEditorViewModel(IClipboard clipboardService)
    {
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));

        _textBuffer = new TextBuffer();
        var syntaxHighlighter = new SyntaxHighlighter();
        RenderManager = new RenderManager(_textBuffer, syntaxHighlighter, 20, 10);
        RenderManager.InvalidateRequested += OnInvalidateRequested;

        // _languageServerClient = new LanguageServerClient("path/to/language-server", "--stdio");
        _undoRedoManager = new UndoRedoManager();
        _cursorManager = new CursorManager(_textBuffer);
        _selectionManager = new SelectionManager(_textBuffer, _cursorManager);
        _inputManager = new InputManager(_textBuffer, _cursorManager, _selectionManager);

        CanvasWidth = 800;
        CanvasHeight = 600;

        InitializeCommands();
    }

    private void OnInvalidateRequested(object? sender, EventArgs e)
    {
        Console.WriteLine("OnInvalidateRequested called");
        RequestInvalidateVisual();
    }

    public RenderManager RenderManager { get; }
    public event EventHandler InvalidateVisualRequested;

    public void RequestInvalidateVisual()
    {
        Console.WriteLine("RequestInvalidateVisual called");
        InvalidateVisualRequested?.Invoke(this, EventArgs.Empty);
    }

    public double CanvasWidth
    {
        get => _canvasWidth;
        set => this.RaiseAndSetIfChanged(ref _canvasWidth, value);
    }

    public double CanvasHeight
    {
        get => _canvasHeight;
        set => this.RaiseAndSetIfChanged(ref _canvasHeight, value);
    }

    public ICommand UndoCommand { get; private set; }
    public ICommand RedoCommand { get; private set; }
    public ICommand CutCommand { get; private set; }
    public ICommand CopyCommand { get; private set; }
    public ICommand PasteCommand { get; private set; }

    private void InitializeCommands()
    {
        UndoCommand = ReactiveCommand.Create(Undo);
        RedoCommand = ReactiveCommand.Create(Redo);
        CutCommand = ReactiveCommand.Create(Cut);
        CopyCommand = ReactiveCommand.Create(Copy);
        PasteCommand = ReactiveCommand.Create(Paste);
    }

    public void Render(DrawingContext context, Size size)
    {
        RenderManager.UpdateViewport(size);
        RenderManager.Render(context, size);
    }

    public void UpdateScrollPosition(double verticalOffset)
    {
        RenderManager.SetScrollPosition(verticalOffset);
    }

    public void Undo()
    {
        _undoRedoManager.Undo();
        RenderManager.Invalidate();
    }

    public void Redo()
    {
        _undoRedoManager.Redo();
        RenderManager.Invalidate();
    }

    public async void Cut()
    {
        var selectedText = _textBuffer.GetSelectedText();
        if (!string.IsNullOrEmpty(selectedText))
        {
            _undoRedoManager.Execute(() =>
                {
                    _textBuffer.DeleteSelectedText();
                    _clipboardService.SetTextAsync(selectedText);
                },
                () => _textBuffer.InsertTextAtCursor(selectedText)
            );
            RenderManager.Invalidate();
        }
    }

    public async Task Copy()
    {
        var selectedText = _textBuffer.GetSelectedText();
        if (!string.IsNullOrEmpty(selectedText)) await _clipboardService.SetTextAsync(selectedText);
    }

    public async Task Paste()
    {
        var clipboardText = await _clipboardService.GetTextAsync();
        if (!string.IsNullOrEmpty(clipboardText))
        {
            _undoRedoManager.Execute(
                () => _textBuffer.InsertTextAtCursor(clipboardText),
                () => _textBuffer.DeleteTextAtCursor(clipboardText.Length)
            );
            RenderManager.Invalidate();
        }
    }

    public async void RequestCompletion(Point position)
    {
        var completions = await _languageServerClient.RequestCompletionAsync(
            "file:///current/document.txt",
            new Position
            {
                Line = (int)(position.Y / RenderManager.LineHeight),
                Character = (int)(position.X / RenderManager.CharWidth)
            }
        );

        DisplayCompletions(completions);
    }

    public async void RequestHover(Point position)
    {
        var hover = await _languageServerClient.RequestHoverAsync(
            "file:///current/document.txt",
            new Position
            {
                Line = (int)(position.Y / RenderManager.LineHeight),
                Character = (int)(position.X / RenderManager.CharWidth)
            }
        );

        DisplayHover(hover);
    }

    public void HandlePointerPressed(Point position)
    {
        _selectionManager.HandlePointerPressed(position.X, position.Y, RenderManager.CharWidth,
            RenderManager.LineHeight);
        RenderManager.Invalidate();
    }

    public void HandlePointerMoved(Point position, bool isLeftButtonPressed)
    {
        _selectionManager.HandlePointerMoved(position.X, position.Y, RenderManager.CharWidth, RenderManager.LineHeight,
            isLeftButtonPressed);
        RenderManager.Invalidate();
    }

    public void HandlePointerReleased(Point position)
    {
        _selectionManager.HandlePointerReleased(position.X, position.Y, RenderManager.CharWidth,
            RenderManager.LineHeight);
        RenderManager.Invalidate();
    }

    public void HandleKeyPress(KeyEventArgs e)
    {
        _inputManager.HandleKeyPress(e);
        RenderManager.Invalidate();
    }

    public void HandleTextInput(string text)
    {
        Console.WriteLine("Text:" + _textBuffer.GetFullText());
        _inputManager.HandleTextInput(text);
        InvalidateVisual();
    }

    private void InvalidateVisual()
    {
        InvalidateVisualRequested.Invoke(this, EventArgs.Empty);
    }

    private void DisplayCompletions(CompletionList completions)
    {
        foreach (var item in completions.Items) Console.WriteLine($"{item.Label}: {item.Detail}");
    }

    private void DisplayHover(Hover hover)
    {
        if (hover.Contents.Value is MarkupContent markupContent)
        {
            Console.WriteLine(markupContent.Value);
        }
        else if (hover.Contents.Value is SumType<string, MarkedString>[] multipleContent)
        {
            foreach (var content in multipleContent)
                if (content.Value is string str)
                    Console.WriteLine(str);
                else if (content.Value is MarkedString markedString) Console.WriteLine(markedString.Value);
        }
        else if (hover.Contents.Value is SumType<string, MarkedString> singleContent)
        {
            if (singleContent.Value is string str)
                Console.WriteLine(str);
            else if (singleContent.Value is MarkedString markedString) Console.WriteLine(markedString.Value);
        }
    }
}