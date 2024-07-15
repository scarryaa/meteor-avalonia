using System.ComponentModel;
using System.Runtime.CompilerServices;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Events;

namespace meteor.ViewModels;

public class TextEditorViewModel : ITextEditorViewModel
{
    private ITextBuffer _textBuffer;
    private readonly IClipboardService _clipboardService;
    private readonly IUndoRedoManager<ITextBuffer> _undoRedoManager;
    private readonly ICursorManager _cursorManager;
    private readonly ISelectionHandler _selectionHandler;
    private int? _cachedLongestLineLength;
    private int _cursorPosition;
    private int _selectionStart = -1;
    private int _selectionEnd = -1;
    private bool _isSelecting;
    private double _windowWidth;
    private double _windowHeight;

    public TextEditorViewModel(ITextBuffer textBuffer, IClipboardService clipboardService,
        IUndoRedoManager<ITextBuffer> undoRedoManager, ICursorManager cursorManager, ISelectionHandler selectionHandler)
    {
        _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _undoRedoManager = undoRedoManager ?? throw new ArgumentNullException(nameof(undoRedoManager));
        _cursorManager = cursorManager ?? throw new ArgumentNullException(nameof(cursorManager));
        _selectionHandler = selectionHandler ?? throw new ArgumentNullException(nameof(selectionHandler));

        _textBuffer.TextChanged += OnTextChanged;
    }

    public int LongestLineLength
    {
        get
        {
            if (!_cachedLongestLineLength.HasValue) _cachedLongestLineLength = CalculateLongestLineLength();
            return _cachedLongestLineLength.Value;
        }
    }

    private int CalculateLongestLineLength()
    {
        var maxLength = 0;
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

    public double FontSize { get; set; } = 13;
    public IScrollableTextEditorViewModel ParentViewModel { get; set; }

    public ITextBuffer TextBuffer
    {
        get => _textBuffer;
        set
        {
            if (_textBuffer != value)
            {
                _textBuffer = value;
                OnPropertyChanged();
            }
        }
    }

    public double CharWidth { get; set; } = 7; // TODO calculate this based on font size

    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            _windowWidth = value;
            OnInvalidateRequired();
        }
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set
        {
            _windowHeight = value;
            OnInvalidateRequired();
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

    public event EventHandler<TextChangedEventArgs> TextChanged;
    public event EventHandler CursorPositionChanged;
    public event EventHandler SelectionChanged;
    public event EventHandler InvalidateRequired;

    public void OnSelectionChanged(int selectionStart, int selectionEnd)
    {
        SelectionStart = selectionStart;
        SelectionEnd = selectionEnd;
        OnSelectionChanged();
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
        else if (_cursorManager.Position < _textBuffer.Length) _textBuffer.DeleteText(_cursorManager.Position, 1);
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

    public void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
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

    public void UpdateLineStarts()
    {
        // TODO update line starts
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        InvalidateLongestLine();
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

    public void Dispose()
    {
        // TODO release managed resources here
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}