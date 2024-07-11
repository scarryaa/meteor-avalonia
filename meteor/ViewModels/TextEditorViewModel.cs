using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Interfaces;
using meteor.Models;
using meteor.Views.Enums;
using meteor.Views.Services;
using meteor.Views.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using ReactiveUI;
using CompletionItem = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionItem;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using SelectionChangedEventArgs = meteor.Models.SelectionChangedEventArgs;
using TextChangedEventArgs = meteor.Views.Models.TextChangedEventArgs;
using Timer = System.Timers.Timer;

namespace meteor.ViewModels;

public class TextEditorViewModel : ViewModelBase, IDisposable
{
    private const double DefaultFontSize = 13;
    private const int DefaultCompletionDebounceTime = 300;
    private const double BaseLineHeight = 20;
    public readonly CompletionService CompletionService;
    
    private LspState _lspState = LspState.NotInitialized;
    private readonly Lazy<ISyntaxHighlighter> _syntaxHighlighter;
    private readonly ICursorPositionService _cursorPositionService;
    private readonly IClipboardService _clipboardService;
    private readonly LineCountViewModel _lineCountViewModel;
    private readonly TaskCompletionSource<bool> _lspInitializationTcs = new();
    private readonly SemaphoreSlim _lspInitLock = new(1, 1);
    private readonly Queue<TextChangedEventArgs> _pendingChanges = new();
    private readonly Dictionary<string, List<SyntaxToken>> _syntaxTokenCache = new();

    private ITextBuffer _textBuffer;
    private long _cursorPosition;
    private long _selectionStart = -1;
    private long _selectionEnd = -1;
    private bool _isSelecting;
    private double _windowHeight;
    private double _windowWidth;
    private long _desiredColumn;
    private double _charWidth;
    private bool _charWidthNeedsUpdate = true;
    private FontFamily _fontFamily;
    private double _longestLineWidth;
    private long _longestLineIndex = -1;
    private bool _longestLineWidthNeedsUpdate = true;
    private double _fontSize = DefaultFontSize;
    private double _lineHeight = BaseLineHeight;
    private ViewModelBase? _parentViewModel;
    private string? _filePath;
    private Timer? _debounceTimer;
    private bool _isLspInitialized;
    private bool _isLspInitializationFailed;
    private bool _isLspInitializationInProgress;
    private Timer? _completionTimer;
    private int _completionDebounceTime = DefaultCompletionDebounceTime;
    private CancellationTokenSource _completionCancellationTokenSource;
    private bool _isPopupVisible;
    private double _popupLeft;
    private double _popupTop;
    private CompletionPopupViewModel _completionPopupViewModel;
    private bool _isApplyingSuggestion;
    public CursorManager CursorManager { get; }
    public SelectionManager SelectionManager { get; }
    public TextManipulator TextManipulator { get; set; }
    public ClipboardManager ClipboardManager { get; }
    public LineManager LineManager { get; }
    public TextEditorUtils TextEditorUtils { get; }
    public UndoRedoManager<TextState> UndoRedoManager { get; }
    public ScrollableTextEditorViewModel? ScrollableViewModel { get; set; }
    public InputManager InputManager { get; }
    public ScrollManager? ScrollManager { get; set; }
    public ScrollableTextEditorViewModel? ScrollableTextEditorViewModel;
    public bool HasUserStartedTyping { get; set; }
    public LspClient LspClient { get; set; }
    public FontPropertiesViewModel FontPropertiesViewModel { get; }

    public TextEditorViewModel(
        LspClient lspClient,
        ICursorPositionService cursorPositionService,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel,
        ITextBuffer textBuffer,
        IClipboardService clipboardService,
        ISyntaxHighlighter syntaxHighlighter,
        IConfiguration configuration,
        ViewModelBase? parentViewModel,
        string? filePath = null)
    {
        _completionPopupViewModel = new CompletionPopupViewModel(this);
        LspClient = lspClient;
        _cursorPositionService = cursorPositionService;
        FontPropertiesViewModel = fontPropertiesViewModel;
        FontFamily = FontFamily.DefaultFontFamilyName;
        _lineCountViewModel = lineCountViewModel;
        _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _syntaxHighlighter = new Lazy<ISyntaxHighlighter>(() => syntaxHighlighter);
        ScrollableTextEditorViewModel = parentViewModel as ScrollableTextEditorViewModel;
        ParentViewModel = parentViewModel;
        _filePath = filePath;
        Console.WriteLine($"TextEditorViewModel initialized with file path: {_filePath}");
        TextBuffer.TextChanged += OnTextChanged;
        CompletionService = new CompletionService(this);
        
        CursorManager = new CursorManager(this);
        SelectionManager = new SelectionManager(this);
        ScrollManager = new ScrollManager(this);
        TextManipulator = new TextManipulator();
        ClipboardManager = new ClipboardManager(this, clipboardService);
        LineManager = new LineManager();
        TextEditorUtils = new TextEditorUtils(this);
        UndoRedoManager = new UndoRedoManager<TextState>(new TextState("", 0));
        InputManager = new InputManager();

        this.WhenAnyValue(x => x.FontPropertiesViewModel.FontFamily).Subscribe(font => FontFamily = font);
        this.WhenAnyValue(x => x.FontPropertiesViewModel.FontSize).Subscribe(size => FontSize = size);
        this.WhenAnyValue(x => x.FontPropertiesViewModel.LineHeight).Subscribe(height => LineHeight = height);

        CompletionPopupViewModel.SuggestionApplied += OnSuggestionApplied;
        LspClient.LogReceived += (sender, log) => Console.WriteLine(log);
        UpdateLineStarts();
    }

    public string? FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    public ViewModelBase? ParentViewModel
    {
        get => _parentViewModel;
        set => this.RaiseAndSetIfChanged(ref _parentViewModel, value);
    }

    public double PopupLeft
    {
        get => _popupLeft;
        set => this.RaiseAndSetIfChanged(ref _popupLeft, value);
    }

    public double PopupTop
    {
        get => _popupTop;
        set => this.RaiseAndSetIfChanged(ref _popupTop, value);
    }

    private void OnSuggestionApplied(object sender, CompletionItem item)
    {
        ApplySelectedSuggestion(item);
    }

    public CompletionPopupViewModel CompletionPopupViewModel
    {
        get => _completionPopupViewModel;
        set => this.RaiseAndSetIfChanged(ref _completionPopupViewModel, value);
    }

    public double LongestLineWidth
    {
        get
        {
            if (_longestLineWidthNeedsUpdate) UpdateLongestLineWidth();
            return _longestLineWidth;
        }
        private set => this.RaiseAndSetIfChanged(ref _longestLineWidth, value);
    }

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set
        {
            this.RaiseAndSetIfChanged(ref _fontFamily, value);
            _charWidthNeedsUpdate = true;
            this.RaisePropertyChanged(nameof(CharWidth));
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            this.RaiseAndSetIfChanged(ref _fontSize, value);
            _charWidthNeedsUpdate = true;
            this.RaisePropertyChanged(nameof(CharWidth));
        }
    }

    public double LineHeight
    {
        get => FontPropertiesViewModel.LineHeight;
        set => this.RaiseAndSetIfChanged(ref _lineHeight, value);
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set => this.RaiseAndSetIfChanged(ref _windowHeight, value);
    }

    public double WindowWidth
    {
        get => _windowWidth;
        set => this.RaiseAndSetIfChanged(ref _windowWidth, value);
    }

    public long CursorPosition
    {
        get => _cursorPosition;
        set
        {
            if (_cursorPosition != value)
            {
                _cursorPosition = value;
                _cursorPositionService.UpdateCursorPosition(_cursorPosition, _textBuffer.LineStarts,
                    _textBuffer.GetLineLength(_textBuffer.LineCount));
                this.RaisePropertyChanged();
            }
        }
    }

    public long SelectionStart
    {
        get => _selectionStart;
        set
        {
            if (_selectionStart != value)
            {
                this.RaiseAndSetIfChanged(ref _selectionStart, value);
                NotifySelectionChanged(_selectionStart);
            }
        }
    }

    public long SelectionEnd
    {
        get => _selectionEnd;
        set
        {
            if (_selectionEnd != value)
            {
                this.RaiseAndSetIfChanged(ref _selectionEnd, value);
                NotifySelectionChanged(null, _selectionEnd);
            }
        }
    }

    public bool IsSelecting
    {
        get => _isSelecting;
        set => this.RaiseAndSetIfChanged(ref _isSelecting, value);
    }

    public long LineCount => _textBuffer.LineCount;
    public double TotalHeight => _textBuffer.TotalHeight;

    public ITextBuffer TextBuffer
    {
        get => _textBuffer;
        set
        {
            this.RaiseAndSetIfChanged(ref _textBuffer, value);
            this.RaisePropertyChanged(nameof(LineCount));
            this.RaisePropertyChanged(nameof(TotalHeight));
            _lineCountViewModel.LineCount = _textBuffer.LineCount;
        }
    }

    public double CharWidth
    {
        get
        {
            if (_charWidthNeedsUpdate) UpdateCharWidth();
            return _charWidth;
        }
        set
        {
            if (_charWidth != value)
            {
                this.RaiseAndSetIfChanged(ref _charWidth, value);
                _charWidthNeedsUpdate = false;
            }
        }
    }

    public bool ShouldScrollToCursor { get; set; } = true;

    public LineCache LineCache { get; } = new();

    public event EventHandler<SelectionChangedEventArgs> SelectionChanged;
    public event EventHandler WidthChanged;
    public event EventHandler LineChanged;
    public event EventHandler? InvalidateRequired;
    public event EventHandler? RequestFocus;
    public event EventHandler? WidthRecalculationRequired;

    public async Task SetFilePath(string? filePath, string content)
    {
        _filePath = filePath;
        this.RaisePropertyChanged(nameof(FilePath));

        if (filePath != null)
        {
            var version = 1;
            var languageId = GetLanguageIdFromFileExtension(filePath);

            Console.WriteLine($"Detected language ID: {languageId}");

            await EnsureLspInitializedAsync();
            await LspClient.SendDidOpenAsync(filePath, languageId, version, content);
        }
    }

    public Point GetCaretScreenPosition(Control textEditorControl, long? position = null)
    {
        var actualPosition = position ?? _cursorPosition;
        var lineIndex = _textBuffer.GetLineIndexFromPosition(actualPosition);
        var lineStart = _textBuffer.GetLineStartPosition((int)lineIndex);
        var charIndex = (int)(actualPosition - lineStart);
        var linePosition = new Point(charIndex * CharWidth, lineIndex * LineHeight);
        var screenPosition = textEditorControl.PointToScreen(linePosition);
        return new Point(screenPosition.X, screenPosition.Y);
    }

    public void ApplySelectedSuggestion(CompletionItem selectedItem)
    {
        try
        {
            _isApplyingSuggestion = true;

            var (wordStart, wordEnd) = GetWordBoundariesAtCursor();
            var replacementText = selectedItem.InsertText ?? selectedItem.Label;

            ReplaceTextRange(wordStart, wordEnd, replacementText);
            CursorPosition = wordStart + replacementText.Length;
            HideCompletionSuggestions();

            ScrollableTextEditorViewModel.ParentRenderManager.InvalidateLines((int)CursorPosition,
                (int)CursorPosition);
        }
        finally
        {
            _isApplyingSuggestion = false;
        }
    }

    public async Task RequestHoverAsync(long position)
    {
        if (_lspState != LspState.Initialized) return;

        var parameters = new
        {
            textDocument = new TextDocumentIdentifier { Uri = new Uri(FilePath) },
            position = GetLspPosition(position)
        };

        var result = await LspClient.RequestHoverAsync(parameters.textDocument.Uri.ToString(), parameters.position);
        HandleHoverResult(result);
    }

    public async Task InsertText(long position, string text)
    {
        var startLine = _textBuffer.GetLineIndexFromPosition(position);
        _textBuffer.InsertText(position, text);
        CursorPosition = position + text.Length;
        var endLine = _textBuffer.GetLineIndexFromPosition(CursorPosition);

        startLine = Math.Max(0, startLine - 1);
        endLine = Math.Min(_textBuffer.LineCount - 1, endLine + 1);
        ScrollableTextEditorViewModel.ParentRenderManager.InvalidateLines((int)startLine, (int)endLine);
    }

    public async Task DeleteText(long start, long length)
    {
        var startLine = _textBuffer.GetLineIndexFromPosition(start);
        var endLine = _textBuffer.GetLineIndexFromPosition(start + length);

        _textBuffer.DeleteText(start, length);
        UpdateLineStartsAfterDeletion(start, length);
        UpdateLongestLineWidthAfterDeletion(start, length);
        NotifyGutterOfLineChange();
        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(TotalHeight));

        _cursorPositionService.UpdateCursorPosition(CursorPosition, _textBuffer.LineStarts,
            _textBuffer.GetLineLength(_textBuffer.LineCount));

        ScrollableTextEditorViewModel.ParentRenderManager.InvalidateLines((int)startLine, (int)endLine);
    }

    public void HideCompletionSuggestions()
    {
        CompletionService.HideCompletionSuggestions();
    }

    public void ShowCompletionSuggestions(CompletionItem[] items)
    {
        CompletionService.ShowCompletionSuggestions(items);
    }

    public void ClearSelection()
    {
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }

    public void NotifyGutterOfLineChange()
    {
        LineChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateGutterWidth()
    {
        WidthChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateServices(TextEditorViewModel viewModel)
    {
        var services = new Dictionary<string, Action<TextEditorViewModel>>
        {
            { nameof(ScrollManager), vm => ScrollManager?.UpdateViewModel(viewModel) },
            { nameof(TextEditorUtils), vm => TextEditorUtils?.UpdateViewModel(viewModel) },
            { nameof(InputManager), vm => InputManager?.UpdateViewModel(viewModel) },
            { nameof(ClipboardManager), vm => ClipboardManager?.UpdateViewModel(viewModel) },
            { nameof(CursorManager), vm => CursorManager?.UpdateViewModel(viewModel) },
            { nameof(SelectionManager), vm => SelectionManager?.UpdateViewModel(viewModel) },
            { nameof(TextManipulator), vm => TextManipulator?.UpdateViewModel(viewModel) },
            { nameof(LineManager), vm => LineManager?.UpdateViewModel(viewModel) }
        };

        foreach (var service in services)
            if (service.Value == null)
                Console.WriteLine($"{service.Key} is null");
            else
                service.Value(viewModel);
    }

    public void UpdateLineCache(long position, string insertedText)
    {
        var startLine = _textBuffer.GetLineIndexFromPosition(position);
        var endLine = _textBuffer.GetLineIndexFromPosition(position + insertedText.Length);

        LineCache.InvalidateRange(startLine, endLine);
    }

    public void UpdateLineCacheAfterDeletion(long start, long length)
    {
        var startLine = _textBuffer.GetLineIndexFromPosition(start);
        var endLine = _textBuffer.GetLineIndexFromPosition(start + length);

        LineCache.InvalidateRange(startLine, endLine);
    }

    public void Focus()
    {
        RequestFocus?.Invoke(this, EventArgs.Empty);
    }

    public T? GetParentViewModel<T>() where T : ViewModelBase
    {
        var current = ParentViewModel;
        while (current != null)
        {
            if (current is T targetViewModel) return targetViewModel;
            if (current is TextEditorViewModel textEditorViewModel) current = textEditorViewModel.ParentViewModel;
        }

        return null;
    }

    public void Dispose()
    {
        LspClient?.Dispose();
    }

    private (long start, long end) GetWordBoundariesAtCursor()
    {
        var lineIndex = TextBuffer.GetLineIndexFromPosition(CursorPosition);
        var lineStart = TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineText = TextBuffer.GetLineText(lineIndex);
        var positionInLine = CursorPosition - lineStart;

        var wordStart = positionInLine;
        while (wordStart > 0 && char.IsLetterOrDigit(lineText[(int)wordStart - 1])) wordStart--;

        var wordEnd = positionInLine;
        while (wordEnd < lineText.Length && char.IsLetterOrDigit(lineText[(int)wordEnd])) wordEnd++;

        return (lineStart + wordStart, lineStart + wordEnd);
    }

    private void ReplaceTextRange(long start, long end, string replacementText)
    {
        var length = end - start;
        TextManipulator.ReplaceText(start, length, replacementText);
    }

    public long GetWordStartPosition(long position)
    {
        var lineIndex = _textBuffer.GetLineIndexFromPosition(position);
        var lineStart = _textBuffer.GetLineStartPosition((int)lineIndex);
        var lineText = _textBuffer.GetLineText(lineIndex);
        var positionInLine = position - lineStart;

        var wordStart = positionInLine;
        while (wordStart > 0 && char.IsLetterOrDigit(lineText[(int)wordStart - 1])) wordStart--;

        return lineStart + wordStart;
    }

    private void ReplaceCurrentWord(string currentWord, string replacementText)
    {
        var lineIndex = TextBuffer.GetLineIndexFromPosition(CursorPosition);
        var lineStart = TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineText = TextBuffer.GetLineText(lineIndex);
        var positionInLine = CursorPosition - lineStart;

        var wordStart = (int)positionInLine;
        while (wordStart > 0 && IsWordChar(lineText[wordStart - 1])) wordStart--;

        var wordEnd = (int)positionInLine;
        while (wordEnd < lineText.Length && IsWordChar(lineText[wordEnd])) wordEnd++;

        var startPosition = lineStart + wordStart;
        var length = wordEnd - wordStart;

        TextManipulator.ReplaceText(startPosition, length, replacementText);
        CursorPosition = startPosition + replacementText.Length;
    }

    private bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    public string GetWordBeforeCursor()
    {
        var lineText = _textBuffer.GetLineText(_textBuffer.GetLineIndexFromPosition(CursorPosition));
        var lineStartPosition =
            _textBuffer.GetLineStartPosition((int)_textBuffer.GetLineIndexFromPosition(CursorPosition));
        var positionInLine = CursorPosition - lineStartPosition;
        var wordStart = positionInLine;

        while (wordStart > 0 && IsWordChar(lineText[(int)wordStart - 1])) wordStart--;

        return lineText.Substring((int)wordStart, (int)(positionInLine - wordStart));
    }

    public void HandleKeyPress()
    {
        Focus();
    }

    private async void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        try
        {
            if (_isApplyingSuggestion) return;

            if (_lspState == LspState.NotInitialized)
            {
                Console.WriteLine("LSP client not initialized. Starting initialization...");
                await EnsureLspInitializedAsync();
            }

            if (_lspState == LspState.Initializing)
            {
                Console.WriteLine("Waiting for LSP initialization to complete...");
                await _lspInitializationTcs.Task;
            }

            if (_lspState == LspState.Initialized && HasUserStartedTyping)
            {
                Console.WriteLine("LSP client initialized. Sending didChange notification...");

                await SendDidChangeNotificationAsync(e);

                var isBackspace = e.DeletedLength > 0 && e.InsertedText.Length == 0;
                var wordBeforeCursor = GetWordBeforeCursor();

                if (isBackspace)
                {
                    if (string.IsNullOrEmpty(wordBeforeCursor))
                        // If the current word is empty after backspace, hide suggestions
                        CompletionService.HideCompletionSuggestions();
                    else
                        // Otherwise, update suggestions
                        await CompletionService.RequestCompletionAsync(e.Position);
                }
                else
                {
                    char? lastTypedChar = e.InsertedText.Length > 0 ? e.InsertedText[^1] : null;
                    await CompletionService.DebouncedRequestCompletionAsync(e.Position, lastTypedChar);
                }
            }
            else
            {
                Console.WriteLine($"LSP client not initialized. Skipping didChange notification. State: {_lspState}");
            }

            ScrollableTextEditorViewModel?.ParentRenderManager.InvalidateLines((int)e.Position, (int)e.Position);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnTextChanged: {ex.Message}");
        }
    }

    public Position GetLspPosition(long position)
    {
        var line = (int)TextBuffer.GetLineIndexFromPosition(position);
        var character = (int)(position - TextBuffer.GetLineStartPosition(line));
        return new Position { Line = line, Character = character };
    }

    private async Task SendDidChangeNotificationAsync(TextChangedEventArgs e)
    {
        try
        {
            var parameters = new
            {
                textDocument = new VersionedTextDocumentIdentifier
                {
                    Uri = new Uri(FilePath),
                    Version = e.Version
                },
                contentChanges = new[]
                {
                    new TextDocumentContentChangeEvent
                    {
                        Range = new Range
                        {
                            Start = GetLspPosition(e.Position),
                            End = GetLspPosition((long)(e.Position + e.DeletedLength))
                        },
                        RangeLength = e.DeletedLength,
                        Text = e.InsertedText
                    }
                }
            };

            Console.WriteLine($"Sending didChange notification for {FilePath}");
            await LspClient.SendDidChangeAsync(parameters.textDocument.Uri.ToString(),
                _textBuffer.GetText(0, _textBuffer.Length), parameters.textDocument.Version);
            Console.WriteLine("didChange notification sent successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending didChange notification: {ex.Message}");
        }
    }

    private void UpdateLongestLineWidth()
    {
        double maxWidth = 0;
        long maxWidthIndex = -1;

        for (var i = 0; i < TextBuffer.LineCount; i++)
        {
            var lineText = TextBuffer.GetLineText(i);
            var lineWidth = MeasureLineWidth(lineText);

            if (lineWidth > maxWidth)
            {
                maxWidth = lineWidth;
                maxWidthIndex = i;
            }
        }

        LongestLineWidth = maxWidth;
        _longestLineIndex = maxWidthIndex;
        _longestLineWidthNeedsUpdate = false;
    }

    private double MeasureLineWidth(string lineText)
    {
        var formattedText = new FormattedText(
            lineText,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily),
            FontSize,
            Brushes.Black);

        return formattedText.Width;
    }

    private void UpdateCharWidth()
    {
        try
        {
            var formattedText = new FormattedText(
                "x",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily),
                FontSize,
                Brushes.Black);
            CharWidth = formattedText.Width;
        }
        catch (Exception ex)
        {
            CharWidth = 10;
        }
    }

    private void UpdateLineStartsAfterDeletion(long start, long length)
    {
        var startLine = _textBuffer.GetLineIndexFromPosition(start);
        var endLine = _textBuffer.GetLineIndexFromPosition(start + length);

        for (var i = startLine; i <= endLine; i++)
        {
            var lineStart = _textBuffer.GetLineStartPosition((int)i);
            _textBuffer.SetLineStartPosition((int)i, lineStart - length);
        }

        _textBuffer.UpdateLineCache();
    }

    private void UpdateLongestLineWidthAfterInsertion(long position, string insertedText)
    {
        var affectedLineIndex = TextBuffer.GetLineIndexFromPosition(position);
        var newLineWidth = MeasureLineWidth(TextBuffer.GetLineText(affectedLineIndex));

        if (newLineWidth > LongestLineWidth)
        {
            LongestLineWidth = newLineWidth;
            _longestLineIndex = affectedLineIndex;
        }
        else if (affectedLineIndex == _longestLineIndex)
        {
            _longestLineWidthNeedsUpdate = true;
        }
    }

    private void UpdateLongestLineWidthAfterDeletion(long start, long length)
    {
        var startLineIndex = TextBuffer.GetLineIndexFromPosition(start);
        var endLineIndex = TextBuffer.GetLineIndexFromPosition(start + length);

        if (startLineIndex <= _longestLineIndex && _longestLineIndex <= endLineIndex)
        {
            _longestLineWidthNeedsUpdate = true;
        }
        else if (startLineIndex == _longestLineIndex)
        {
            var newLineWidth = MeasureLineWidth(TextBuffer.GetLineText(startLineIndex));
            if (newLineWidth < LongestLineWidth)
                _longestLineWidthNeedsUpdate = true;
            else
                LongestLineWidth = newLineWidth;
        }
    }

    private async Task EnsureLspInitializedAsync()
    {
        await _lspInitLock.WaitAsync();
        try
        {
            if (!_isLspInitialized)
            {
                Console.WriteLine("Initializing LSP client...");
                await LspClient.InitializeAsync();
                _isLspInitialized = true;
                _lspState = LspState.Initialized;
                Console.WriteLine("LSP client initialized successfully.");
            }
            else
            {
                Console.WriteLine("LSP client already initialized.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing LSP client: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _lspState = LspState.Failed;
        }
        finally
        {
            _lspInitLock.Release();
        }
    }

    public bool IsLspReady()
    {
        if (_lspState != LspState.Initialized)
        {
            Console.WriteLine($"LSP not ready. Current state: {_lspState}");
            return false;
        }

        return true;
    }

    private void HandleHoverResult(object result)
    {
        // Parse and display hover information
    }

    public void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }

    public void NotifySelectionChanged(long? selectionStart = null, long? selectionEnd = null)
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(selectionStart, selectionEnd));
    }

    public void OnWidthRecalculationRequired()
    {
        WidthRecalculationRequired?.Invoke(this, EventArgs.Empty);
        UpdateGutterWidth();
    }

    public void UpdateLineStarts()
    {
        _textBuffer.UpdateLineCache();
    }

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        if (path.StartsWith("~"))
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = path.Replace("~", homePath);
        }

        return Path.GetFullPath(path);
    }

    private string GetLanguageIdFromFileExtension(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".ts" => "typescript",
            ".tsx" => "typescriptreact",
            ".js" => "javascript",
            ".jsx" => "javascriptreact",
            ".json" => "json",
            ".html" => "html",
            ".css" => "css",
            _ => "typescript"
        };
    }
}