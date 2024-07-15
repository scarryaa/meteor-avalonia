using System.ComponentModel;
using System.Runtime.CompilerServices;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Events;

namespace meteor.ViewModels;

public class TextEditorViewModel : ITextEditorViewModel, IDisposable
{
    private ITextBuffer _textBuffer;
    private readonly IClipboardService _clipboardService;
    private readonly IUndoRedoManager<ITextBuffer> _undoRedoManager;
    private readonly ICursorManager _cursorManager;
    private readonly ISelectionHandler _selectionHandler;
    private readonly ITextMeasurer _textMeasurer;
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
        IGutterViewModel gutterViewModel)
    {
        _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _undoRedoManager = undoRedoManager ?? throw new ArgumentNullException(nameof(undoRedoManager));
        _cursorManager = cursorManager ?? throw new ArgumentNullException(nameof(cursorManager));
        _selectionHandler = selectionHandler ?? throw new ArgumentNullException(nameof(selectionHandler));
        _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        LineCountViewModel = lineCountViewModel ?? throw new ArgumentNullException(nameof(lineCountViewModel));
        GutterViewModel = gutterViewModel ?? throw new ArgumentNullException(nameof(gutterViewModel));

        _textBuffer.TextChanged += OnTextChanged;
        LineHeight = _textMeasurer.GetLineHeight(FontSize, FontFamily);
    }

    public double RequiredWidth => Math.Max(LongestLineWidth, ViewportWidth);
    public double RequiredHeight => Math.Max(TotalHeight, ViewportHeight);

    public string FontFamily { get; set; } = "Consolas";
    public double FontSize { get; set; } = 13;

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

    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            if (_windowWidth != value)
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
            if (_windowHeight != value)
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
            if (_viewportWidth != value)
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
            if (_viewportHeight != value)
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
            if (_longestLineWidth != value)
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
            if (_totalHeight != value)
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
            if (_lineHeight != value)
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
            if (_offset != value)
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
            if (_offset.Y != value)
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
            if (_offset.X != value)
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
            if (!_cachedLongestLineLength.HasValue)
                _cachedLongestLineLength = CalculateLongestLineLength();
            return _cachedLongestLineLength.Value;
        }
    }

    public event EventHandler<TextChangedEventArgs> TextChanged;
    public event EventHandler CursorPositionChanged;
    public event EventHandler SelectionChanged;
    public event EventHandler InvalidateRequired;
    public event PropertyChangedEventHandler PropertyChanged;

    public void UpdateViewProperties()
    {
        UpdateLineStarts();
        UpdateLongestLineWidth();
        UpdateTotalHeight();
    }

    public void UpdateLineStarts()
    {
        // TODO Implement line starts update logic here
    }

    private void UpdateLongestLineWidth()
    {
        var longestLine = TextBuffer.GetText(0, TextBuffer.Length).Split('\n')
            .OrderByDescending(line => _textMeasurer.MeasureWidth(line, FontSize, FontFamily))
            .First();
        Console.WriteLine("Longest line : " + longestLine);
        LongestLineWidth = _textMeasurer.MeasureWidth(longestLine, FontSize, FontFamily);
        OnPropertyChanged(nameof(RequiredWidth));
    }

    private void UpdateTotalHeight()
    {
        TotalHeight = TextBuffer.LineCount * LineHeight;
        Console.WriteLine("Line height : " + LineHeight);
        Console.WriteLine("total lines : " + TextBuffer.LineCount);
        OnPropertyChanged(nameof(RequiredHeight));
    }

    private double CalculateLongestLineLength()
    {
        var maxLength = 0.0;
        for (var i = 0; i < TextBuffer.LineCount; i++)
        {
            var lineLength = TextBuffer.GetLineLength(i);
            if (lineLength > maxLength) maxLength = lineLength;
        }

        return maxLength;
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
        ClearSelection();
        InvalidateLongestLine();
        OnInvalidateRequired();
    }

    public void DeleteText(int start, int length)
    {
        _textBuffer.DeleteText(start, length);
        CursorPosition = start;
        ClearSelection();
        InvalidateLongestLine();
        OnInvalidateRequired();
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
        }
    }

    public void HandleDelete()
    {
        if (_selectionHandler.HasSelection)
            DeleteSelection();
        else if (_cursorManager.Position < _textBuffer.Length)
            _textBuffer.DeleteText(_cursorManager.Position, 1);
    }

    public void InsertNewLine()
    {
        if (_selectionHandler.HasSelection) DeleteSelection();
        _textBuffer.InsertText(_cursorManager.Position, Environment.NewLine);
        _cursorManager.MoveCursorRight(false);
    }

    private void DeleteSelection()
    {
        var start = _selectionHandler.SelectionStart;
        var end = _selectionHandler.SelectionEnd;
        _textBuffer.DeleteText(start, end - start);
        _cursorManager.SetPosition(start);
        _selectionHandler.ClearSelection();
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

    public async Task PasteText()
    {
        var clipboardText = await _clipboardService.GetTextAsync();
        if (!string.IsNullOrEmpty(clipboardText))
        {
            if (_selectionHandler.HasSelection) DeleteSelection();
            _textBuffer.InsertText(_cursorManager.Position, clipboardText);
            _cursorManager.SetPosition(_cursorManager.Position + clipboardText.Length);
        }
    }

    public void StartSelection()
    {
        IsSelecting = true;
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }

    public void UpdateSelection()
    {
        if (IsSelecting)
            SelectionEnd = CursorPosition;
    }

    public void ClearSelection()
    {
        IsSelecting = false;
        SelectionStart = -1;
        SelectionEnd = -1;
    }

    public string GetSelectedText()
    {
        if (!IsSelecting || SelectionStart == SelectionEnd)
            return string.Empty;

        var start = Math.Min(SelectionStart, SelectionEnd);
        var end = Math.Max(SelectionStart, SelectionEnd);
        return TextBuffer.GetText(start, end - start);
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        InvalidateLongestLine();
        UpdateViewProperties();
        TextChanged?.Invoke(this, e);
        OnInvalidateRequired();
    }

    private void OnCursorPositionChanged()
    {
        CursorPositionChanged?.Invoke(this, EventArgs.Empty);
        UpdateSelection();
        OnInvalidateRequired();
    }

    private void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        OnInvalidateRequired();
    }

    public void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _textBuffer.TextChanged -= OnTextChanged;
        // Dispose of other resources as needed
    }
}