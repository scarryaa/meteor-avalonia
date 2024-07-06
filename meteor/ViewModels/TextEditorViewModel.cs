using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Media;
using meteor.Interfaces;
using meteor.Models;
using meteor.Views.Services;
using meteor.Views.Utils;
using ReactiveUI;

namespace meteor.ViewModels;

public class TextEditorViewModel : ViewModelBase
{
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly ICursorPositionService _cursorPositionService;
    private ITextBuffer _textBuffer;
    private long _cursorPosition;
    private long _selectionStart = -1;
    private long _selectionEnd = -1;
    private bool _isSelecting;
    private double _windowHeight;
    private double _windowWidth;
    private long _desiredColumn;
    private readonly LineCountViewModel _lineCountViewModel;
    private double _charWidth;
    private bool _charWidthNeedsUpdate = true;
    private FontFamily _fontFamily;
    private readonly IClipboardService _clipboardService;
    private double _longestLineWidth;
    private long _longestLineIndex = -1;
    private bool _longestLineWidthNeedsUpdate = true;
    private readonly double _lineSpacingFactor = BaseLineHeight / DefaultFontSize;
    private double _fontSize = DefaultFontSize;
    private double _lineHeight = BaseLineHeight;
    private const double DefaultFontSize = 13;
    private ViewModelBase? _parentViewModel;
    private string _filePath;
    
    public const double BaseLineHeight = 20;

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

    public string FilePath
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


    public TextEditorViewModel(
        ICursorPositionService cursorPositionService,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel,
        ITextBuffer textBuffer,
        IClipboardService clipboardService,
        ISyntaxHighlighter syntaxHighlighter,
        ViewModelBase parentViewModel,
        string filePath = null)
    {
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
        _textBuffer.TextChanged += OnLinesUpdated;

        CursorManager = new CursorManager(this);
        SelectionManager = new SelectionManager(this);
        ScrollManager = new ScrollManager(this);
        TextManipulator = new TextManipulator();

        ClipboardManager = new ClipboardManager(this, clipboardService);
        LineManager = new LineManager();
        TextEditorUtils = new TextEditorUtils(this);
        UndoRedoManager = new UndoRedoManager<TextState>(new TextState("", 0));

        InputManager = new InputManager();

        this.WhenAnyValue(x => x.FontPropertiesViewModel.FontFamily)
            .Subscribe(font => FontFamily = font);
        this.WhenAnyValue(x => x.FontPropertiesViewModel.FontSize)
            .Subscribe(size => FontSize = size);
        this.WhenAnyValue(x => x.FontPropertiesViewModel.LineHeight)
            .Subscribe(height => { LineHeight = height; });

        UpdateLineStarts();
        UpdateSyntaxTokens(0, (int)_textBuffer.LineCount - 1);
    }

    public void SetFilePath(string filePath)
    {
        _filePath = filePath;
        Console.WriteLine($"File path set to: {_filePath}");
        this.RaisePropertyChanged(nameof(FilePath));

        if (_scrollableViewModel?.ParentRenderManager != null)
            _scrollableViewModel.ParentRenderManager?.UpdateFilePath(_filePath);

        UpdateSyntaxTokens(0, (int)TextBuffer.LineCount - 1);
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

    public bool  ShouldScrollToCursor { get; set; } = true;

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

    public async Task InitializeAsync()
    {
        await UpdateSyntaxHighlightingAsync();
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

            // Update the entire text buffer's syntax tokens initially
            UpdateSyntaxTokens(0, (int)_textBuffer.LineCount - 1);
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


    public async Task UpdateSyntaxHighlightingAsync()
    {
        var fullText = _textBuffer.GetText(0, _textBuffer.Length);
        SyntaxTokens = await Task.Run(() => _syntaxHighlighter.HighlightSyntax(fullText, _filePath));
        OnInvalidateRequired();
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

    public async Task InsertLargeTextAsync(long position, string text)
    {
        await Task.Run(() =>
        {
            _textBuffer.InsertText(position, text);
            UpdateLineCache(position, text);
        });

        CursorPosition = position + text.Length;
        NotifyGutterOfLineChange();
        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(TotalHeight));
        OnInvalidateRequired();
        await Task.Run(UpdateLongestLineWidth);
        OnWidthRecalculationRequired();
        await UpdateSyntaxHighlightingAsync();
    }

    public async Task DeleteLargeTextAsync(long start, long length)
    {
        await Task.Run(() =>
        {
            _textBuffer.DeleteText(start, length);
            UpdateLineCacheAfterDeletion(start, length);
        });

        NotifyGutterOfLineChange();
        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(TotalHeight));
        OnInvalidateRequired();
        await Task.Run(UpdateLongestLineWidth);
        OnWidthRecalculationRequired();
        await UpdateSyntaxHighlightingAsync();
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

    public async Task PasteText()
    {
        var clipboardText = await _clipboardService.GetTextAsync();
        if (!string.IsNullOrEmpty(clipboardText))
        {
            var position = CursorPosition;
            var startLine = _textBuffer.GetLineIndexFromPosition(position);
            InsertText(position, clipboardText);
            var endLine = _textBuffer.GetLineIndexFromPosition(position + clipboardText.Length);

            UpdateGutterWidth();

            // Update syntax tokens for the affected range
            UpdateSyntaxTokens((int)startLine, (int)endLine);
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

    private async Task OnTextChanged()
    {
        var text = _textBuffer.GetText(0, _textBuffer.Length);
        await _scrollableViewModel.ParentRenderManager.UpdateSyntaxHighlightingAsync(text);
        OnInvalidateRequired();
    }
    
    public async Task InsertText(long position, string text)
    {
        var startLine = _textBuffer.GetLineIndexFromPosition(position);
        var endLine = _textBuffer.GetLineIndexFromPosition(position + text.Length);
        
        _textBuffer.InsertText(position, text);
        CursorPosition = position + text.Length;
        UpdateLongestLineWidthAfterInsertion(position, text);
        NotifyGutterOfLineChange();
        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(TotalHeight));
        OnWidthRecalculationRequired();
        UpdateGutterWidth();
        _scrollableViewModel?.ParentRenderManager?.InvalidateLines((int)startLine, (int)endLine);

        Console.WriteLine($"Updating syntax tokens for lines {startLine} to {endLine}");
        UpdateSyntaxTokens((int)startLine, (int)endLine);

        _cursorPositionService.UpdateCursorPosition(CursorPosition, _textBuffer.LineStarts,
            _textBuffer.GetLineLength(_textBuffer.LineCount));

        _scrollableViewModel?.ParentRenderManager?.InvalidateLines((int)startLine, (int)endLine);
        _scrollableViewModel?.ParentRenderManager.UpdateSyntaxHighlightingAsync(text);
        OnInvalidateRequired();
        OnTextChanged();
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
        await UpdateSyntaxHighlightingAsync();

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
        // Get the range of updated lines
        var updatedRange = _textBuffer.GetUpdatedRange();
        UpdateLineStarts();
        this.RaisePropertyChanged(nameof(TotalHeight));
        OnWidthRecalculationRequired();
        UpdateGutterWidth();
        UpdateSyntaxTokens(updatedRange.StartLine, updatedRange.EndLine);
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
            if (current is T targetViewModel)
                return targetViewModel;
            if (current is TextEditorViewModel textEditorViewModel) current = textEditorViewModel.ParentViewModel;
        }

        return null;
    }

    public void UpdateSyntaxTokens(int startLine, int endLine)
    {
        Console.WriteLine($"UpdateSyntaxTokens called for lines {startLine} to {endLine}");
        var text = _textBuffer.GetText(0, _textBuffer.Length);
        List<SyntaxToken> newTokens;

        if (startLine == 0 && endLine == _textBuffer.LineCount - 1)
            // Full file update
            newTokens = _syntaxHighlighter.HighlightSyntax(text, _filePath);
        else
            // Partial update
            newTokens = _syntaxHighlighter.HighlightSyntax(text, startLine, endLine, _filePath);

        Console.WriteLine($"Received {newTokens.Count} new tokens from SyntaxHighlighter");

        // Update the syntax tokens for the specified range
        SyntaxTokens.RemoveAll(token => token.Line >= startLine && token.Line <= endLine);
        SyntaxTokens.AddRange(newTokens);

        Console.WriteLine($"Total tokens after update: {SyntaxTokens.Count}");

        // Notify the view to invalidate and redraw the affected lines
        OnInvalidateRequired();
    }

    public List<SyntaxToken> SyntaxTokens { get; private set; } = new();
}