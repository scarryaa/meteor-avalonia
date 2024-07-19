using System.Text;
using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.Events;

namespace meteor.Core.Services;

public class InputService : IInputService
{
    private readonly ITabService _tabService;
    private readonly ICursorService _cursorService;
    private readonly ITextAnalysisService _textAnalysisService;
    private readonly ISelectionService _selectionService;
    private readonly IClipboardService _clipboardService;
    private readonly ITextMeasurer _textMeasurer;
    private DateTime _lastClickTime = DateTime.MinValue;
    private int _clickCount;
    private bool _isSelecting;
    private int _selectionStart = -1;
    private readonly StringBuilder _stringBuilder = new();
    private double _verticalScrollOffset;
    private double _horizontalScrollOffset;

    public InputService(
        ITabService tabService,
        ICursorService cursorService,
        ITextAnalysisService textAnalysisService,
        ISelectionService selectionService,
        IClipboardService clipboardService,
        ITextMeasurer textMeasurer)
    {
        _tabService = tabService;
        _cursorService = cursorService;
        _textAnalysisService = textAnalysisService;
        _selectionService = selectionService;
        _clipboardService = clipboardService;
        _textMeasurer = textMeasurer;
    }

    public void UpdateScrollOffset(double verticalScrollOffset, double horizontalScrollOffset)
    {
        _verticalScrollOffset = verticalScrollOffset;
        _horizontalScrollOffset = horizontalScrollOffset;
    }

    public void InsertText(string text)
    {
        var textBufferService = _tabService.GetActiveTextBufferService();
        var (selectionStart, selectionLength) = _selectionService.GetSelection();
        if (selectionLength != 0) DeleteSelectedText();

        var cursorPosition = _cursorService.GetCursorPosition();
        cursorPosition = Math.Clamp(cursorPosition, 0, textBufferService.Length);
        textBufferService.Insert(cursorPosition, text);
        _cursorService.SetCursorPosition(cursorPosition + text.Length);
        _selectionService.ClearSelection();
    }

    public void DeleteText(int index, int length)
    {
        var textBufferService = _tabService.GetActiveTextBufferService();
        textBufferService.Delete(index, length);
    }

    public void HandleTextInput(TextInputEventArgs e)
    {
        InsertText(e.Text);
    }
    
    public async Task HandleKeyDown(Key key, KeyModifiers? modifiers = null)
    {
        var isShiftPressed = modifiers.HasValue && modifiers.Value.HasFlag(KeyModifiers.Shift);
        var isCtrlPressed = modifiers.HasValue && modifiers.Value.HasFlag(KeyModifiers.Ctrl);

        switch (key)
        {
            case Key.Backspace:
                HandleBackspace(isCtrlPressed);
                break;
            case Key.Delete:
                HandleDelete(isCtrlPressed);
                break;
            case Key.Enter:
                InsertText("\n");
                break;
            case Key.Left:
            case Key.Right:
            case Key.Up:
            case Key.Down:
                HandleArrowKeys(key, isShiftPressed, isCtrlPressed);
                break;
            case Key.A:
                if (isCtrlPressed) HandleSelectAll();
                else InsertText("A");
                break;
            case Key.Home:
            case Key.End:
                HandleHomeEnd(key, isShiftPressed, isCtrlPressed);
                break;
            case Key.PageUp:
            case Key.PageDown:
                HandlePageUpDown(key, isShiftPressed);
                break;
            case Key.Tab:
                HandleTab(isShiftPressed);
                break;
            case Key.C:
                if (isCtrlPressed) HandleCopy();
                break;
            case Key.X:
                if (isCtrlPressed) HandleCut();
                break;
            case Key.V:
                if (isCtrlPressed) await HandlePaste();
                break;
            default:
                if (key >= Key.A && key <= Key.Z) InsertText(key.ToString());
                break;
        }
    }

    private void HandleBackspace(bool isCtrlPressed)
    {
        var textBufferService = _tabService.GetActiveTextBufferService();
        var (selectionStart, selectionLength) = _selectionService.GetSelection();
        if (selectionLength != 0)
        {
            DeleteSelectedText();
        }
        else
        {
            var cursorPosition = _cursorService.GetCursorPosition();
            if (cursorPosition > 0)
            {
                if (isCtrlPressed)
                {
                    var wordStart = _textAnalysisService.GetWordStart(textBufferService, cursorPosition - 1);
                    DeleteText(wordStart, cursorPosition - wordStart);
                    _cursorService.SetCursorPosition(wordStart);
                }
                else
                {
                    DeleteText(cursorPosition - 1, 1);
                    _cursorService.SetCursorPosition(cursorPosition - 1);
                }
            }
        }
    }

    private void HandleDelete(bool isCtrlPressed)
    {
        var textBufferService = _tabService.GetActiveTextBufferService();
        var (selectionStart, selectionLength) = _selectionService.GetSelection();
        if (selectionLength != 0)
        {
            DeleteSelectedText();
        }
        else
        {
            var cursorPosition = _cursorService.GetCursorPosition();
            if (cursorPosition < textBufferService.Length)
            {
                if (isCtrlPressed)
                {
                    var wordEnd = _textAnalysisService.GetWordEnd(textBufferService, cursorPosition);
                    DeleteText(cursorPosition, wordEnd - cursorPosition);
                }
                else
                {
                    DeleteText(cursorPosition, 1);
                }
            }
        }
    }

    private void HandleArrowKeys(Key key, bool isShiftPressed, bool isCtrlPressed)
    {
        var currentPosition = _cursorService.GetCursorPosition();
        int newPosition;

        switch (key)
        {
            case Key.Left:
                newPosition = isCtrlPressed
                    ? _textAnalysisService.GetWordStart(_tabService.GetActiveTextBufferService(), currentPosition)
                    : Math.Max(0, currentPosition - 1);
                break;
            case Key.Right:
                newPosition = isCtrlPressed
                    ? _textAnalysisService.GetWordEnd(_tabService.GetActiveTextBufferService(), currentPosition)
                    : Math.Min(_tabService.GetActiveTextBufferService().Length, currentPosition + 1);
                break;
            case Key.Up:
                newPosition = GetPositionAbove(currentPosition);
                break;
            case Key.Down:
                newPosition = GetPositionBelow(currentPosition);
                break;
            default:
                return;
        }

        if (newPosition != currentPosition)
        {
            if (isShiftPressed)
            {
                var (selectionStart, _) = _selectionService.GetSelection();
                if (selectionStart == -1) _selectionService.StartSelection(currentPosition);
                _selectionService.UpdateSelection(newPosition);
            }
            else
            {
                _selectionService.ClearSelection();
            }
            _cursorService.SetCursorPosition(newPosition);
        }
    }

    private void HandleSelectAll()
    {
        var textBufferService = _tabService.GetActiveTextBufferService();
        var textLength = textBufferService.Length;
        _selectionService.SetSelection(0, textLength);
        _cursorService.SetCursorPosition(textLength);
    }

    private void HandleHomeEnd(Key key, bool isShiftPressed, bool isCtrlPressed)
    {
        var textBufferService = _tabService.GetActiveTextBufferService();
        var currentPosition = _cursorService.GetCursorPosition();
        int newPosition;

        if (key == Key.Home)
        {
            newPosition = isCtrlPressed
                ? _textAnalysisService.GetLineStart(textBufferService, currentPosition)
                : 0;
        }
        else // Key.End
        {
            newPosition = isCtrlPressed
                ? _textAnalysisService.GetLineEnd(textBufferService, currentPosition)
                : textBufferService.Length;
        }

        if (isShiftPressed)
            _selectionService.UpdateSelection(newPosition);
        else
            _selectionService.ClearSelection();

        _cursorService.SetCursorPosition(newPosition);
    }

    private void HandlePageUpDown(Key key, bool isShiftPressed)
    {
        const int linesToMove = 10;
        var currentPosition = _cursorService.GetCursorPosition();
        var newPosition = currentPosition;

        for (var i = 0; i < linesToMove; i++)
            newPosition = key == Key.PageUp
                ? GetPositionAbove(newPosition)
                : GetPositionBelow(newPosition);

        if (isShiftPressed)
            _selectionService.UpdateSelection(newPosition);
        else
            _selectionService.ClearSelection();

        _cursorService.SetCursorPosition(newPosition);
    }

    private void HandleTab(bool isShiftPressed)
    {
        var textBufferService = _tabService.GetActiveTextBufferService();
        if (isShiftPressed)
        {
            var (selectionStart, selectionLength) = _selectionService.GetSelection();
            var lineStart = GetLineStart(selectionStart);

            if (textBufferService[lineStart] == '\t')
            {
                DeleteText(lineStart, 1);
                _selectionService.SetSelection(selectionStart - 1, selectionLength - 1);
            }
        }
        else
        {
            InsertText("\t");
        }
    }

    private void HandleCopy()
    {
        var (selectionStart, selectionLength) = _selectionService.GetSelection();
        if (selectionLength != 0)
        {
            _stringBuilder.Clear();
            var textBufferService = _tabService.GetActiveTextBufferService();
            textBufferService.GetTextSegment(selectionStart, selectionLength, _stringBuilder);
            _clipboardService.SetText(_stringBuilder.ToString());
        }
    }

    private void HandleCut()
    {
        HandleCopy();
        DeleteSelectedText();
    }

    private async Task HandlePaste()
    {
        var textToPaste = await _clipboardService.GetText();
        if (!string.IsNullOrEmpty(textToPaste)) InsertText(textToPaste);
    }

    public int GetNewCursorPosition(Key key, int currentPosition)
    {
        return key switch
        {
            Key.Left => Math.Max(0, currentPosition - 1),
            Key.Right => Math.Min(_tabService.GetActiveTextBufferService().Length, currentPosition + 1),
            Key.Up => GetPositionAbove(currentPosition),
            Key.Down => GetPositionBelow(currentPosition),
            _ => currentPosition
        };
    }

    private int GetPositionAbove(int currentPosition)
    {
        var textBufferService = _tabService.GetActiveTextBufferService();
        var currentLineStart = _textAnalysisService.GetLineStart(textBufferService, currentPosition);

        if (currentLineStart == 0)
            return currentPosition;

        var prevLineStart = _textAnalysisService.GetLineStart(textBufferService, currentLineStart - 1);
        var columnInCurrentLine = currentPosition - currentLineStart;

        var prevLineEnd = currentLineStart - 1;
        var prevLineLength = prevLineEnd - prevLineStart + 1;

        return prevLineStart + Math.Min(columnInCurrentLine, prevLineLength);
    }

    private int GetPositionBelow(int currentPosition)
    {
        var textBufferService = _tabService.GetActiveTextBufferService();
        var (_, lineEnd) = _textAnalysisService.GetLineBoundaries(textBufferService, currentPosition);
        if (lineEnd == textBufferService.Length) return currentPosition;

        var nextLineStart = lineEnd + 1;
        var nextLineEnd = textBufferService.IndexOf('\n', nextLineStart);
        nextLineEnd = nextLineEnd == -1 ? textBufferService.Length : nextLineEnd;

        var currentLineStart = _textAnalysisService.GetLineStart(textBufferService, currentPosition);
        var columnInLine = currentPosition - currentLineStart;
        var nextLineLength = nextLineEnd - nextLineStart;

        return nextLineStart + Math.Min(columnInLine, nextLineLength);
    }

    public void HandlePointerPressed(PointerPressedEventArgs e)
    {
        int index;
        if (e.X != 0 || e.Y != 0)
        {
            index = ClampIndex(_textMeasurer.GetIndexAtPosition(_tabService.GetActiveTextBufferService(), e.X, e.Y,
                _verticalScrollOffset, _horizontalScrollOffset));
        }
        else
        {
            index = ClampIndex(e.Index);
        }

        if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            _clickCount++;
        else
            _clickCount = 1;
        _lastClickTime = DateTime.Now;

        switch (_clickCount)
        {
            case 1:
                HandleSingleClick(index);
                break;
            case 2:
                HandleDoubleClick(index);
                break;
            case 3:
                HandleTripleClick(index);
                _clickCount = 0;
                break;
        }

        _isSelecting = true;
        _selectionStart = index;
    }

    public void HandlePointerMoved(PointerEventArgs e)
    {
        if (!_isSelecting) return;

        var index = ClampIndex(_textMeasurer.GetIndexAtPosition(_tabService.GetActiveTextBufferService(), e.X, e.Y,
            _verticalScrollOffset, _horizontalScrollOffset));

        if (e.IsLeftButtonPressed)
        {
            SetSelectionAndCursor(_selectionStart, index);
        }
        else
        {
            // If the left button is not pressed, end the selection
            _isSelecting = false;
            _selectionStart = -1;
        }
    }

    public void HandlePointerReleased(PointerReleasedEventArgs e)
    {
        _isSelecting = false;
        _selectionStart = -1;
    }

    private void HandleSingleClick(int index)
    {
        _cursorService.SetCursorPosition(index);
        _selectionService.ClearSelection();
        _selectionService.StartSelection(index);
    }

    private void HandleDoubleClick(int index)
    {
        var (start, end) = _textAnalysisService.GetWordBoundaries(_tabService.GetActiveTextBufferService(), index);
        SetSelectionAndCursor(start, end);
    }

    private void HandleTripleClick(int index)
    {
        var (start, end) = _textAnalysisService.GetLineBoundaries(_tabService.GetActiveTextBufferService(), index);
        SetSelectionAndCursor(start, end);
    }
    
    private void SetSelectionAndCursor(int start, int end)
    {
        start = ClampIndex(start);
        end = ClampIndex(end);
        _selectionService.SetSelection(start, end - start);
        _cursorService.SetCursorPosition(end);
        _selectionStart = start;
    }

    private int ClampIndex(int index)
    {
        return Math.Clamp(index, 0, _tabService.GetActiveTextBufferService().Length);
    }

    private void DeleteSelectedText()
    {
        var (selectionStart, selectionLength) = _selectionService.GetSelection();
        if (selectionLength != 0)
        {
            selectionStart = Math.Max(0, Math.Min(selectionStart, _tabService.GetActiveTextBufferService().Length));
            var endIndex = Math.Max(0,
                Math.Min(selectionStart + selectionLength, _tabService.GetActiveTextBufferService().Length));
            _tabService.GetActiveTextBufferService().Delete(selectionStart, endIndex - selectionStart);
            _cursorService.SetCursorPosition(selectionStart);
            _selectionService.ClearSelection();
        }
    }

    private int GetLineStart(int position)
    {
        while (position > 0 && _tabService.GetActiveTextBufferService()[position - 1] != '\n') position--;
        return position;
    }

    private int GetLineEnd(int position)
    {
        while (position < _tabService.GetActiveTextBufferService().Length &&
               _tabService.GetActiveTextBufferService()[position] != '\n') position++;
        return position;
    }

    private int GetWordStart(int position)
    {
        while (position > 0 && !char.IsWhiteSpace(_tabService.GetActiveTextBufferService()[position - 1])) position--;
        return position;
    }

    private int GetWordEnd(int position)
    {
        while (position < _tabService.GetActiveTextBufferService().Length &&
               !char.IsWhiteSpace(_tabService.GetActiveTextBufferService()[position])) position++;
        return position;
    }
}
