using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.Interfaces;
using meteor.Models;
using meteor.Views;
using meteor.Views.Enums;
using meteor.Views.Services;
using meteor.Views.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using ReactiveUI;
using CompletionItem = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionItem;
using File = System.IO.File;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using SelectionChangedEventArgs = meteor.Models.SelectionChangedEventArgs;
using TextChangedEventArgs = meteor.Views.Models.TextChangedEventArgs;
using Timer = System.Timers.Timer;

namespace meteor.ViewModels;

public class TextEditorViewModel : ViewModelBase
{
    private LspState _lspState = LspState.NotInitialized;
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly ICursorPositionService _cursorPositionService;
    private readonly IClipboardService _clipboardService;
    private readonly LineCountViewModel _lineCountViewModel;
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
    private readonly double _lineSpacingFactor = BaseLineHeight / DefaultFontSize;
    private double _fontSize = DefaultFontSize;
    private double _lineHeight = BaseLineHeight;
    private const double DefaultFontSize = 13;
    private ViewModelBase? _parentViewModel;
    private string? _filePath;
    private readonly LspClient _lspClient;
    private Timer? _debounceTimer;
    private readonly TaskCompletionSource<bool> _lspInitializationTcs = new();
    private bool _isLspInitialized;
    private readonly SemaphoreSlim _lspInitLock = new(1, 1);
    private bool _isLspInitializationFailed;
    private bool _isLspInitializationInProgress;
    private readonly Queue<TextChangedEventArgs> _pendingChanges = new();
    private Timer? _completionTimer;
    private const int DefaultCompletionDebounceTime = 300;
    private int _completionDebounceTime = DefaultCompletionDebounceTime;
    private CancellationTokenSource _completionCancellationTokenSource;
    private bool _isPopupVisible;
    private double _popupLeft;
    private double _popupTop;
    private CompletionPopupViewModel _completionPopupViewModel;
    private Action<double, double> _showPopup;
    private Action _hidePopup;
    private CompletionPopup _popupWindow;
    private bool _isApplyingSuggestion;
    
    public const double BaseLineHeight = 20;

    public RenderManager RenderManager { get; set; }
    public CursorManager CursorManager { get; }
    public SelectionManager SelectionManager { get; }
    public TextManipulator TextManipulator { get; set; }
    public ClipboardManager ClipboardManager { get; }
    public LineManager LineManager { get; }
    public TextEditorUtils TextEditorUtils { get; }
    public UndoRedoManager<TextState> UndoRedoManager { get; }
    public ScrollableTextEditorViewModel? _scrollableViewModel;
    public InputManager InputManager { get; }
    public ScrollManager ScrollManager { get; set; }

    public bool HasUserStartedTyping { get; set; }

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

    public FontPropertiesViewModel FontPropertiesViewModel { get; }

    public double PopupLeft
    {
        get => _popupLeft;
        set
        {
            this.RaiseAndSetIfChanged(ref _popupLeft, value);
            ShowPopup(_popupLeft, PopupTop);
        }
    }

    public double PopupTop
    {
        get => _popupTop;
        set
        {
            this.RaiseAndSetIfChanged(ref _popupTop, value);
            ShowPopup(PopupLeft, _popupTop);
        }
    }

    public bool IsPopupVisible
    {
        get => _isPopupVisible;
        set
        {
            this.RaiseAndSetIfChanged(ref _isPopupVisible, value);
            if (_isPopupVisible)
                ShowPopup(PopupLeft, PopupTop);
            else
                HidePopup();
        }
    }

    private void ShowPopup(double left, double top)
    {
        if (_popupWindow == null || _popupWindow.IsVisible == false)
        {
            _popupWindow = new CompletionPopup();
            _popupWindow.DataContext = CompletionPopupViewModel;
        }

        _popupWindow.SetPosition(left, top);
        _popupWindow.IsVisible = true;
        CompletionPopupViewModel.IsFocused = true;
        _popupWindow.IsHitTestVisible = true;
    }

    private void HidePopup()
    {
        _popupWindow?.Hide();
        CompletionPopupViewModel.IsFocused = false;
        _popupWindow!.IsHitTestVisible = false;
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
            OnInvalidateRequired();
        }
        finally
        {
            _isApplyingSuggestion = false;
        }
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

    private string GetCurrentWord()
    {
        var lineIndex = TextBuffer.GetLineIndexFromPosition(CursorPosition);
        var lineStart = TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineText = TextBuffer.GetLineText(lineIndex);
        var positionInLine = CursorPosition - lineStart;

        var wordStart = (int)positionInLine;
        while (wordStart > 0 && char.IsLetterOrDigit(lineText[wordStart - 1])) wordStart--;

        var wordEnd = (int)positionInLine;
        while (wordEnd < lineText.Length && char.IsLetterOrDigit(lineText[wordEnd])) wordEnd++;

        return lineText.Substring(wordStart, wordEnd - wordStart);
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

    public void HideCompletionSuggestions()
    {
        IsPopupVisible = false;
        CompletionPopupViewModel.IsFocused = false;
    }

    public CompletionPopupViewModel CompletionPopupViewModel
    {
        get => _completionPopupViewModel;
        set => this.RaiseAndSetIfChanged(ref _completionPopupViewModel, value);
    }

    
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
        _completionPopupViewModel = new CompletionPopupViewModel();
        _lspClient = lspClient;
        _cursorPositionService = cursorPositionService;
        FontPropertiesViewModel = fontPropertiesViewModel;
        FontFamily = FontFamily.DefaultFontFamilyName;
        _lineCountViewModel = lineCountViewModel;
        _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _syntaxHighlighter = syntaxHighlighter;
        ParentViewModel = parentViewModel;
        _filePath = filePath;
        Console.WriteLine($"TextEditorViewModel initialized with file path: {_filePath}");
        TextBuffer.TextChanged += OnTextChanged;

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

        _lspClient.LogReceived += (sender, log) => Console.WriteLine(log);
        UpdateLineStarts();

        _popupWindow = new CompletionPopup();
        _popupWindow.DataContext = _completionPopupViewModel;
    }

    public Point GetCaretScreenPosition(Control textEditorControl)
    {
        var lineIndex = _textBuffer.GetLineIndexFromPosition(_cursorPosition);
        var lineStart = _textBuffer.GetLineStartPosition((int)lineIndex);
        var charIndex = (int)(_cursorPosition - lineStart);
        var linePosition = new Point(charIndex * CharWidth, lineIndex * LineHeight);
        var screenPosition = textEditorControl.PointToScreen(linePosition);
        return new Point(screenPosition.X, screenPosition.Y);
    }

    private async Task EnsureLspInitializedAsync()
    {
        await _lspInitLock.WaitAsync();
        try
        {
            if (!_isLspInitialized)
            {
                Console.WriteLine("Initializing LSP client...");
                await _lspClient.InitializeAsync();
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

    private bool IsLspReady()
    {
        if (_lspState != LspState.Initialized)
        {
            Console.WriteLine($"LSP not ready. Current state: {_lspState}");
            return false;
        }

        return true;
    }

    public async Task RequestCompletionAsync(long position, char? lastTypedChar = null)
    {
        if (!HasUserStartedTyping) return;

        try
        {
            if (!IsLspReady()) return;

            // Check if the completion popup is already visible or if the last typed character is a trigger
            if (!IsPopupVisible && !IsCompletionTriggerCharacter(lastTypedChar)) return;

            var lspPosition = GetLspPosition(position);
            var lineText = _textBuffer.GetLineText(_textBuffer.GetLineIndexFromPosition(position));
            var triggerCharacter = lastTypedChar?.ToString() ??
                                   (position > 0 ? _textBuffer.GetText(position - 1, 1) : null);

            Console.WriteLine($"Requesting completion at position {position} for {FilePath}");
            Console.WriteLine($"Line text: {lineText}");
            Console.WriteLine($"Trigger character: {triggerCharacter}");

            var currentLine = _textBuffer.GetLineText(_textBuffer.GetLineIndexFromPosition(position));
            var precedingText = currentLine.Substring(0,
                (int)(position -
                      _textBuffer.GetLineStartPosition((int)_textBuffer.GetLineIndexFromPosition(position))));

            var context = new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = triggerCharacter
            };

            if (IsMethodInvocationContext(precedingText))
            {
                context.TriggerKind = CompletionTriggerKind.TriggerCharacter;
                context.TriggerCharacter = "(";
            }
            else if (IsMemberAccessContext(precedingText))
            {
                context.TriggerKind = CompletionTriggerKind.TriggerCharacter;
                context.TriggerCharacter = ".";
            }

            var result = await _lspClient.RequestCompletionAsync(FilePath, lspPosition, context);

            if (result.Items == null || result.Items.Length == 0)
            {
                Console.WriteLine("No completion items received.");
                ClearCompletionItems();
                return;
            }

            var wordBeforeCursor = GetWordBeforeCursor();

            var filteredItems = result.Items
                .Where(item => IsRelevantCompletionItem(item, wordBeforeCursor))
                .Take(20)
                .ToArray();

            if (filteredItems.Length > 0)
            {
                result.Items = filteredItems;
                HandleCompletionResult(result);
            }
            else
            {
                Console.WriteLine("No relevant completion items after filtering.");
                ClearCompletionItems();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting completion: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            ClearCompletionItems();
        }
    }

    private bool IsCompletionTriggerCharacter(char? c)
    {
        if (c == null) return false;
        return char.IsLetterOrDigit(c.Value) || c == '.' || c == '_';
    }

    private void ClearCompletionItems()
    {
        CompletionPopupViewModel.UpdateCompletionItems(Array.Empty<CompletionItem>());
        HideCompletionSuggestions();
    }

    private bool IsMethodInvocationContext(string precedingText)
    {
        return precedingText.TrimEnd().EndsWith("(");
    }

    private bool IsMemberAccessContext(string precedingText)
    {
        return precedingText.TrimEnd().EndsWith(".");
    }

    private bool IsRelevantCompletionItem(CompletionItem item, string wordBeforeCursor)
    {
        if (string.IsNullOrEmpty(wordBeforeCursor)) return true;

        var label = item.Label.ToLowerInvariant();
        var word = wordBeforeCursor.ToLowerInvariant();

        // Fuzzy matching
        if (FuzzyMatch(label, word)) return true;

        // Camel case matching
        if (label.Contains(word, StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    private bool FuzzyMatch(string label, string word)
    {
        var i = 0;
        foreach (var c in word)
        {
            i = label.IndexOf(c, i);
            if (i == -1)
                return false;
            i++;
        }

        return true;
    }

    private string GetWordBeforeCursor()
    {
        var lineText =
            _textBuffer.GetLineText(
                _textBuffer.GetLineIndexFromPosition(_scrollableViewModel.TextEditorViewModel.CursorPosition));
        var lineStartPosition =
            _textBuffer.GetLineStartPosition(
                (int)_textBuffer.GetLineIndexFromPosition(_scrollableViewModel.TextEditorViewModel.CursorPosition));
        var positionInLine = _scrollableViewModel.TextEditorViewModel.CursorPosition - lineStartPosition;
        var wordStart = positionInLine;

        while (wordStart > 0 && char.IsLetterOrDigit(lineText[(int)wordStart - 1])) wordStart--;

        return lineText.Substring((int)wordStart, (int)(positionInLine - wordStart));
    }

    private void HandleCompletionResult(CompletionList result)
    {
        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (result.Items != null && result.Items.Any())
                {
                    var relevantItems = result.Items
                        .Where(item => IsRelevantCompletionItem(item, GetWordBeforeCursor()))
                        .ToArray();

                    ShowCompletionSuggestions(relevantItems);
                }
                else
                {
                    ShowCompletionSuggestions(Array.Empty<CompletionItem>());
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling completion result: {ex.Message}");
        }
    }

    public void ShowCompletionSuggestions(CompletionItem[] items)
    {
        CompletionPopupViewModel.UpdateCompletionItems(items);
        IsPopupVisible = true;
    }

    public async Task RequestHoverAsync(long position)
    {
        if (_lspState != LspState.Initialized) return;

        var parameters = new
        {
            textDocument = new TextDocumentIdentifier { Uri = new Uri(FilePath) },
            position = GetLspPosition(position)
        };

        var result = await _lspClient.RequestHoverAsync(parameters.textDocument.Uri.ToString(), parameters.position);
        HandleHoverResult(result);
    }

    private void HandleHoverResult(object result)
    {
        // Parse and display hover information
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
            await _lspClient.SendDidChangeAsync(parameters.textDocument.Uri.ToString(),
                _textBuffer.GetText(0, _textBuffer.Length),
                parameters.textDocument.Version);
            Console.WriteLine("didChange notification sent successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending didChange notification: {ex.Message}");
        }
    }

    public async Task SetFilePath(string? filePath)
    {
        _filePath = filePath;
        Console.WriteLine($"File path set to: {_filePath}");
        this.RaisePropertyChanged(nameof(FilePath));

        if (_filePath != null)
        {
            var text = await File.ReadAllTextAsync(_filePath);
            var languageId = GetLanguageIdFromFileExtension(_filePath);
            var version = 1;

            Console.WriteLine($"Detected language ID: {languageId}");

            await EnsureLspInitializedAsync();
            await _lspClient.SendDidOpenAsync(_filePath, languageId, version, text);
        }

        // if (_scrollableViewModel?.ParentRenderManager != null)
        //     await _scrollableViewModel.ParentRenderManager.UpdateFilePathAsync(_filePath);
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
            _ => "typescript" // Default to TypeScript if unknown
        };
    }

    private async Task DebouncedRequestCompletionAsync(long position, char? lastTypedChar)
    {
        if (_completionCancellationTokenSource != null)
        {
            _completionCancellationTokenSource.Cancel();
            _completionCancellationTokenSource.Dispose();
        }

        _completionCancellationTokenSource = new CancellationTokenSource();
        var token = _completionCancellationTokenSource.Token;

        try
        {
            await Task.Delay(1, token);
            if (!token.IsCancellationRequested) await RequestCompletionAsync(position, lastTypedChar);
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DebouncedRequestCompletionAsync: {ex.Message}");
        }
    }
    
    public event EventHandler<SelectionChangedEventArgs> SelectionChanged;
    public event EventHandler WidthChanged;
    public event EventHandler LineChanged;
    public event EventHandler? InvalidateRequired;
    public event EventHandler? RequestFocus;
    public event EventHandler? WidthRecalculationRequired;

    public double LongestLineWidth
    {
        get
        {
            if (_longestLineWidthNeedsUpdate) UpdateLongestLineWidth();
            return _longestLineWidth;
        }
        private set => this.RaiseAndSetIfChanged(ref _longestLineWidth, value);
    }

    public virtual void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }

    public void Focus()
    {
        RequestFocus?.Invoke(this, EventArgs.Empty);
    }

    public bool ShouldScrollToCursor { get; set; } = true;

    public LineCache LineCache { get; } = new();

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

    public void NotifySelectionChanged(long? selectionStart = null, long? selectionEnd = null)
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(selectionStart, selectionEnd));
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

    protected virtual void OnWidthRecalculationRequired()
    {
        WidthRecalculationRequired?.Invoke(this, EventArgs.Empty);
        UpdateGutterWidth();
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

    public async Task CopyText()
    {
        if (SelectionStart != SelectionEnd)
        {
            var selectedText = _textBuffer.GetText(SelectionStart, SelectionEnd - SelectionStart);
            await _clipboardService.SetTextAsync(selectedText);
        }
    }

    public void NotifyGutterOfLineChange()
    {
        LineChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateGutterWidth()
    {
        WidthChanged?.Invoke(this, EventArgs.Empty);
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

                var changes = new List<TextDocumentContentChangeEvent>
                {
                    new()
                    {
                        Text = e.InsertedText,
                        Range = new Range
                        {
                            Start = GetLspPosition(e.Position),
                            End = GetLspPosition((long)(e.Position + e.DeletedLength))
                        },
                        RangeLength = e.DeletedLength
                    }
                };

                await SendDidChangeNotificationAsync(e);

                // Pass the last typed character to RequestCompletionAsync
                char? lastTypedChar = e.InsertedText.Length > 0 ? e.InsertedText[^1] : null;
                await DebouncedRequestCompletionAsync(e.Position, lastTypedChar);
            }
            else
            {
                Console.WriteLine($"LSP client not initialized. Skipping didChange notification. State: {_lspState}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnTextChanged: {ex.Message}");
        }
    }


    private Position GetLspPosition(long position)
    {
        var line = (int)TextBuffer.GetLineIndexFromPosition(position);
        var character = (int)(position - TextBuffer.GetLineStartPosition(line));
        return new Position { Line = line, Character = character };
    }

    public async Task InsertText(long position, string text)
    {
        var startLine = _textBuffer.GetLineIndexFromPosition(position);
        _textBuffer.InsertText(position, text);
        CursorPosition = position + text.Length;
        var endLine = _textBuffer.GetLineIndexFromPosition(CursorPosition);

        // Expand the range of invalidated lines
        startLine = Math.Max(0, startLine - 1);
        endLine = Math.Min(_textBuffer.LineCount - 1, endLine + 1);

        _scrollableViewModel?.ParentRenderManager?.InvalidateLines((int)startLine, (int)endLine);
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
        _scrollableViewModel?.ParentRenderManager?.InvalidateLines((int)startLine, (int)endLine);

        _cursorPositionService.UpdateCursorPosition(CursorPosition, _textBuffer.LineStarts,
            _textBuffer.GetLineLength(_textBuffer.LineCount));

        _scrollableViewModel?.ParentRenderManager?.InvalidateLines((int)startLine, (int)endLine);
        OnInvalidateRequired();
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

    public void ClearSelection()
    {
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }

    public void UpdateLineStarts()
    {
        _textBuffer.UpdateLineCache();
    }

    private void OnLinesUpdated(object sender, EventArgs e)
    {
        UpdateLineStarts();
        this.RaisePropertyChanged(nameof(TotalHeight));
        OnWidthRecalculationRequired();
        UpdateGutterWidth();
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
}

public static class TaskExtensions
{
    public static void FireAndForget(this Task task)
    {
        task.ContinueWith(t => { Console.WriteLine($"Task failed with exception: {t.Exception}"); },
            TaskContinuationOptions.OnlyOnFaulted);
    }
}