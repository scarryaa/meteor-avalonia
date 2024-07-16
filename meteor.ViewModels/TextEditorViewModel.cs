using System.ComponentModel;
using System.Runtime.CompilerServices;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Events;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using Microsoft.Extensions.Logging;

namespace meteor.ViewModels;

public sealed class TextEditorViewModel : ITextEditorViewModel
{
    private readonly IClipboardService _clipboardService;
    private readonly IUndoRedoManager<ITextBuffer> _undoRedoManager;
    private readonly ICursorManager _cursorManager;
    private readonly ISelectionHandler _selectionHandler;
    private readonly ITextMeasurer _textMeasurer;
    private readonly ILogger<TextEditorViewModel> _logger;
    private readonly IEventAggregator _eventAggregator;

    private ITextBuffer _textBuffer;
    private double? _cachedLongestLineLength;
    private int _cursorPosition;
    private int _selectionStart = -1;
    private int _selectionEnd = -1; 
    private bool _isSelecting;
    private double _windowWidth;
    private double _windowHeight;
    private double _viewportWidth;
    private double _viewportHeight;
    private double _longestLineWidth;
    private double _totalHeight;
    private double _lineHeight;
    private Vector _offset;
    private string _fontFamily = "Consolas";
    private double _fontSize = 13;
    private const double Tolerance = 0.0001;

    public ILineCountViewModel LineCountViewModel { get; }
    public IGutterViewModel GutterViewModel { get; }

    public TextEditorViewModel(
        ITextBuffer textBuffer,
        IClipboardService clipboardService,
        IUndoRedoManager<ITextBuffer> undoRedoManager,
        ICursorManager cursorManager,
        ISelectionHandler selectionHandler,
        ITextMeasurer textMeasurer,
        ILineCountViewModel lineCountViewModel,
        IGutterViewModel gutterViewModel,
        ILogger<TextEditorViewModel> logger,
        IEventAggregator eventAggregator)
    {
        _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _undoRedoManager = undoRedoManager ?? throw new ArgumentNullException(nameof(undoRedoManager));
        _cursorManager = cursorManager ?? throw new ArgumentNullException(nameof(cursorManager));
        _selectionHandler = selectionHandler ?? throw new ArgumentNullException(nameof(selectionHandler));
        _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        LineCountViewModel = lineCountViewModel ?? throw new ArgumentNullException(nameof(lineCountViewModel));
        GutterViewModel = gutterViewModel ?? throw new ArgumentNullException(nameof(gutterViewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        LineHeight = _textMeasurer.GetLineHeight(FontSize, FontFamily);

        SubscribeToEvents();
    }

    public ITextBuffer TextBuffer
    {
        get => _textBuffer;
        set
        {
            if (_textBuffer != value)
            {
                _textBuffer = value;
                OnPropertyChanged();
                UpdateViewProperties();
            }
        }
    }

    public double RequiredWidth => Math.Max(LongestLineWidth, ViewportWidth);
    public double RequiredHeight => Math.Max(TotalHeight, ViewportHeight);

    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            if (_fontFamily != value)
            {
                _fontFamily = value;
                OnPropertyChanged();
                UpdateLineHeight();
            }
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (Math.Abs(_fontSize - value) > Tolerance)
            {
                _fontSize = value;
                OnPropertyChanged();
                UpdateLineHeight();
            }
        }
    }

    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            if (Math.Abs(_windowWidth - value) > Tolerance)
            {
                _windowWidth = value;
                OnPropertyChanged();
                UpdateLongestLineWidth();
            }
        }
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set
        {
            if (Math.Abs(_windowHeight - value) > Tolerance)
            {
                _windowHeight = value;
                OnPropertyChanged();
                UpdateTotalHeight();
            }
        }
    }

    public double ViewportWidth
    {
        get => _viewportWidth;
        set
        {
            if (Math.Abs(_viewportWidth - value) > Tolerance)
            {
                _viewportWidth = value;
                OnPropertyChanged();
                UpdateLongestLineWidth();
            }
        }
    }

    public double ViewportHeight
    {
        get => _viewportHeight;
        set
        {
            if (Math.Abs(_viewportHeight - value) > Tolerance)
            {
                _viewportHeight = value;
                OnPropertyChanged();
                UpdateTotalHeight();
            }
        }
    }

    public double LongestLineWidth
    {
        get => Math.Max(_longestLineWidth, WindowWidth);
        private set
        {
            if (Math.Abs(_longestLineWidth - value) > Tolerance)
            {
                _longestLineWidth = value;
                OnPropertyChanged();
            }
        }
    }

    public double TotalHeight
    {
        get => Math.Max(_totalHeight, WindowHeight);
        private set
        {
            if (Math.Abs(_totalHeight - value) > Tolerance)
            {
                _totalHeight = value;
                OnPropertyChanged();
            }
        }
    }

    public double LineHeight
    {
        get => _lineHeight;
        set
        {
            if (Math.Abs(_lineHeight - value) > Tolerance)
            {
                _lineHeight = value;
                OnPropertyChanged();
                UpdateTotalHeight();
            }
        }
    }

    public Vector Offset
    {
        get => _offset;
        set
        {
            if (Math.Abs(_offset.X - value.X) > Tolerance || Math.Abs(_offset.Y - value.Y) > Tolerance)
            {
                _offset = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VerticalOffset));
                OnPropertyChanged(nameof(HorizontalOffset));
                LineCountViewModel.VerticalOffset = value.Y;
            }
        }
    }

    public double VerticalOffset
    {
        get => _offset.Y;
        set
        {
            if (Math.Abs(_offset.Y - value) > Tolerance)
            {
                _offset = (Vector)_offset.WithY(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Offset));
                LineCountViewModel.VerticalOffset = value;      
            }
        }
    }

    public double HorizontalOffset
    {
        get => _offset.X;
        set
        {
            if (Math.Abs(_offset.X - value) > Tolerance)
            {
                _offset = (Vector)_offset.WithX(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Offset));
            }
        }
    }

    public int CursorPosition
    {
        get => _cursorPosition;
        set
        {
            value = Math.Clamp(value, 0, _textBuffer.Length);
            if (_cursorPosition != value)
            {
                _cursorPosition = value;
                OnCursorPositionChanged();
            }
        }
    }

    public int SelectionStart
    {
        get => _selectionStart;
        set
        {
            if (_selectionStart != value)
            {
                _selectionStart = value;
                OnSelectionChanged();
            }
        }
    }

    public int SelectionEnd
    {
        get => _selectionEnd;
        set
        {
            if (_selectionEnd != value)
            {
                _selectionEnd = value;
                OnSelectionChanged();
            }
        }
    }

    public bool IsSelecting
    {
        get => _isSelecting;
        set
        {
            if (_isSelecting != value)
            {
                _isSelecting = value;
                OnSelectionChanged();
            }
        }
    }

    public double LongestLineLength
    {
        get
        {
            _cachedLongestLineLength ??= CalculateLongestLineLength();
            return _cachedLongestLineLength.Value;
        }
    }

    public event EventHandler<TextChangedEventArgs>? TextChanged;
    public event EventHandler<CursorPositionChangedEventArgs>? CursorPositionChanged;
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler? InvalidateRequired;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void UpdateViewProperties()
    {
        UpdateLineStarts();
        UpdateLongestLineWidth();
        UpdateTotalHeight();
    }

    public void UpdateLineStarts()
    {
        // TODO: Implement line starts update logic here
    }

    private void SubscribeToEvents()
    {
        _eventAggregator.Subscribe<TextChangedEventArgs>(OnTextChanged);
        _eventAggregator.Subscribe<CursorPositionChangedEventArgs>(OnCursorPositionChanged);
        _eventAggregator.Subscribe<SelectionChangedEventArgs>(OnSelectionChanged);
        _eventAggregator.Subscribe<IsSelectingChangedEventArgs>(OnIsSelectingChanged);
        _eventAggregator.Subscribe<TextEditorCommandTextChangedEventArgs>(OnTextEditorCommandTextChanged);
    }

    private void OnTextEditorCommandTextChanged(TextEditorCommandTextChangedEventArgs args)
    {
        UpdateViewProperties();
    }

    private void OnCursorPositionChanged(CursorPositionChangedEventArgs e)
    {
        CursorPosition = e.Position;
        UpdateSelection();
        OnInvalidateRequired();
    }

    private void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        SelectionStart = e.NewStart;
        SelectionEnd = e.NewEnd;
        IsSelecting = e.IsSelecting;
        OnInvalidateRequired();
    }

    private void OnIsSelectingChanged(IsSelectingChangedEventArgs e)
    {
        IsSelecting = e.IsSelecting;
        OnInvalidateRequired();
    }

    public void UpdateLongestLineWidth()
    {
        double maxWidth = 0;
        for (var i = 0; i < TextBuffer.LineCount; i++)
        {
            var line = TextBuffer.GetLineText(i);
            var lineWidth = _textMeasurer.MeasureWidth(line, FontSize, FontFamily);
            if (lineWidth > maxWidth)
            {
                maxWidth = lineWidth;
                _logger.LogDebug($"New longest line (index {i}): {line}");
            }
        }

        LongestLineWidth = maxWidth;
        OnPropertyChanged(nameof(RequiredWidth));
    }

    private void UpdateTotalHeight()
    {
        TotalHeight = TextBuffer.LineCount * LineHeight;
        _logger.LogDebug("Line height : " + LineHeight);
        _logger.LogDebug("total lines : " + TextBuffer.LineCount);
        OnPropertyChanged(nameof(RequiredHeight));
    }

    private void UpdateLineHeight()
    {
        LineHeight = _textMeasurer.GetLineHeight(FontSize, FontFamily);
    }

    private double CalculateLongestLineLength()
    {
        return Enumerable.Range(0, TextBuffer.LineCount)
            .Max(i => TextBuffer.GetLineLength(i));
    }

    public void InvalidateLongestLine()
    {
        _cachedLongestLineLength = null;
        OnPropertyChanged(nameof(LongestLineLength));
    }

    public void InsertText(int position, string text)
    {
        _textBuffer.InsertText(position, text);
        CursorPosition = position + text.Length;
        _selectionHandler.ClearSelection();
        InvalidateLongestLine();
        _eventAggregator.Publish(new TextChangedEventArgs(position, text, 0));
    }

    public void DeleteText(int start, int length)
    {
        _textBuffer.DeleteText(start, length);
        CursorPosition = start;
        ClearSelection();
        InvalidateLongestLine();
        _eventAggregator.Publish(new TextChangedEventArgs(start, "", length));
    }

    public void HandleBackspace()
    {
        if (_selectionHandler.HasSelection)
        {
            DeleteSelection();
        }
        else if (_cursorManager.Position > 0)
        {
            _textBuffer.DeleteText(_cursorManager.Position - 1, 1);
            _cursorManager.MoveCursorLeft(false);
            _eventAggregator.Publish(new TextChangedEventArgs(_cursorManager.Position, "", 1));
            _eventAggregator.Publish(new CursorPositionChangedEventArgs(_cursorManager.Position,
                _textBuffer.GetLineStarts(),
                _textBuffer.GetLineLength(_textBuffer.GetLineIndexFromPosition(_cursorManager.Position))));
        }
    }

    public void HandleDelete()
    {
        if (_selectionHandler.HasSelection)
        {
            DeleteSelection();
        }
        else if (_cursorManager.Position < _textBuffer.Length)
        {
            _textBuffer.DeleteText(_cursorManager.Position, 1);
            _cursorManager.SetPosition(_cursorManager.Position);
            _eventAggregator.Publish(new TextChangedEventArgs(_cursorManager.Position, "", 1));
        }           
    }

    public void InsertNewLine()
    {
        if (_selectionHandler.HasSelection) DeleteSelection();
        _textBuffer.InsertText(_cursorManager.Position, Environment.NewLine);
        _cursorManager.MoveCursorRight(false);
        _eventAggregator.Publish(new TextChangedEventArgs(_cursorManager.Position - Environment.NewLine.Length,
            Environment.NewLine, 0));
        _eventAggregator.Publish(new CursorPositionChangedEventArgs(_cursorManager.Position,
            _textBuffer.GetLineStarts(),
            _textBuffer.GetLineLength(_textBuffer.GetLineIndexFromPosition(_cursorManager.Position))));
    }

    private void DeleteSelection()
    {
        var start = _selectionHandler.SelectionStart;
        var end = _selectionHandler.SelectionEnd;
        _textBuffer.DeleteText(start, end - start);
        _cursorManager.SetPosition(start);
        _selectionHandler.ClearSelection();
        _eventAggregator.Publish(new TextChangedEventArgs(start, "", end - start));
        _eventAggregator.Publish(new CursorPositionChangedEventArgs(start, _textBuffer.GetLineStarts(),
            _textBuffer.GetLineLength(_textBuffer.GetLineIndexFromPosition(start))));
        _eventAggregator.Publish(new SelectionChangedEventArgs(start, -1, false));
        _eventAggregator.Publish(new IsSelectingChangedEventArgs(false));
    }

    public async Task CopyText()
    {
        if (_selectionHandler.HasSelection)
        {
            var selectedText = _textBuffer.GetText(_selectionHandler.SelectionStart,
                _selectionHandler.SelectionEnd - _selectionHandler.SelectionStart);
            await _clipboardService.SetTextAsync(selectedText);
        }
    }

    public void PasteText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var insertPosition = Math.Min(SelectionStart, SelectionEnd);
        var deleteLength = Math.Abs(SelectionEnd - SelectionStart);

        if (insertPosition == -1) insertPosition = CursorPosition;

        if (deleteLength > 0)
        {
            TextBuffer.DeleteText(insertPosition, deleteLength);
        }

        TextBuffer.InsertText(insertPosition, text);

        // Update cursor position and selection
        CursorPosition = insertPosition + text.Length;
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;

        _logger.LogDebug(
            $"After paste: CursorPosition={CursorPosition}, TextLength={TextBuffer.Length}, LineCount={TextBuffer.LineCount}");
    }

    public void StartSelection()
    {
        IsSelecting = true;
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
        _eventAggregator.Publish(new IsSelectingChangedEventArgs(true));
        _eventAggregator.Publish(new SelectionChangedEventArgs(SelectionStart, SelectionEnd, true));
    }

    public void UpdateSelection()
    {
        if (IsSelecting)
        {
            SelectionEnd = Math.Clamp(CursorPosition, 0, _textBuffer.Length);
            NormalizeSelection();
            _selectionHandler.UpdateSelection(SelectionEnd);
            _eventAggregator.Publish(
                new SelectionChangedEventArgs(SelectionStart, SelectionEnd, true));
        }
    }

    private void NormalizeSelection()
    {
        if (SelectionStart > SelectionEnd) (SelectionStart, SelectionEnd) = (SelectionEnd, SelectionStart);
    }

    public void ClearSelection()
    {
        IsSelecting = false;
        SelectionStart = -1;
        SelectionEnd = -1;
        _eventAggregator.Publish(new IsSelectingChangedEventArgs(false));
        _eventAggregator.Publish(new SelectionChangedEventArgs(-1, -1, false));
    }

    public string GetSelectedText()
    {
        if (!IsSelecting || SelectionStart == SelectionEnd)
            return string.Empty;

        var start = Math.Min(SelectionStart, SelectionEnd);
        var end = Math.Max(SelectionStart, SelectionEnd);
        return TextBuffer.GetText(start, end - start);
    }

    private void OnTextChanged(TextChangedEventArgs e)
    {
        InvalidateLongestLine();
        UpdateAffectedLines(e);
        UpdateTotalHeight();
        OnInvalidateRequired();
    }
    
    private void UpdateAffectedLines(TextChangedEventArgs e)
    {
        int startLine = TextBuffer.GetLineIndexFromPosition(e.Position);
        int endLine = TextBuffer.GetLineIndexFromPosition(e.Position + Math.Max(e.InsertedText.Length, e.DeletedLength));

        var maxWidth = LongestLineWidth;
        var lineSpan = new Span<char>(new char[1024]);

        for (int i = startLine; i <= endLine; i++)
        {
            var lineLength = TextBuffer.GetLineLength(i);
            if (lineLength > lineSpan.Length) lineSpan = new Span<char>(new char[lineLength]);

            TextBuffer.GetLineText(i);
            var lineWidth = _textMeasurer.MeasureWidth(new string(lineSpan.Slice(0, lineLength)), FontSize, FontFamily);

            if (lineWidth > maxWidth) maxWidth = lineWidth;
        }

        if (maxWidth > LongestLineWidth)
        {
            LongestLineWidth = maxWidth;
            OnPropertyChanged(nameof(RequiredWidth));
        }
    }

    private void OnCursorPositionChanged()
    {
        CursorPositionChanged?.Invoke(this,
            new CursorPositionChangedEventArgs(CursorPosition, _textBuffer.GetLineStarts(),
                _textBuffer.GetLineLength(_textBuffer.GetLineIndexFromPosition(CursorPosition))));      
        OnInvalidateRequired();
    }

    private void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(SelectionStart, SelectionEnd, IsSelecting));
        OnInvalidateRequired();
    }

    public void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _eventAggregator.Unsubscribe<TextChangedEventArgs>(OnTextChanged);
        _eventAggregator.Unsubscribe<CursorPositionChangedEventArgs>(OnCursorPositionChanged);
        _eventAggregator.Unsubscribe<SelectionChangedEventArgs>(OnSelectionChanged);
        _eventAggregator.Unsubscribe<IsSelectingChangedEventArgs>(OnIsSelectingChanged);
    }
}
