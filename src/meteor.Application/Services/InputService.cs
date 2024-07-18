using meteor.Application.Interfaces;
using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.Events;

namespace meteor.Application.Services;


public class InputService : IInputService
{
    private readonly ITextBufferService _textBufferService;
    private readonly ICursorService _cursorService;
    private readonly ITextAnalysisService _textAnalysisService;
    private readonly ISelectionService _selectionService;
    private readonly IClipboardService _clipboardService;
    private readonly ITextMeasurer _textMeasurer;
    private DateTime _lastClickTime = DateTime.MinValue;
    private int _clickCount;
    private bool _isSelecting;
    private int _selectionStart = -1;

    public InputService(
        ITextBufferService textBufferService,
        ICursorService cursorService,
        ITextAnalysisService textAnalysisService,
        ISelectionService selectionService,
        IClipboardService clipboardService,
        ITextMeasurer textMeasurer)
    {
        _textBufferService = textBufferService;
        _cursorService = cursorService;
        _textAnalysisService = textAnalysisService;
        _selectionService = selectionService;
        _clipboardService = clipboardService;
        _textMeasurer = textMeasurer;
    }

    public void InsertText(string text)
    {
        var (_, selectionLength) = _selectionService.GetSelection();
        if (selectionLength != 0) DeleteSelectedText();

        var cursorPosition = _cursorService.GetCursorPosition();
        cursorPosition = Math.Clamp(cursorPosition, 0, _textBufferService.Length);
        _textBufferService.Insert(cursorPosition, text);
        _cursorService.SetCursorPosition(cursorPosition + text.Length);
        _selectionService.ClearSelection();
    }

    public void DeleteText(int index, int length)
    {
        _textBufferService.Delete(index, length);
    }

    public void HandleTextInput(TextInputEventArgs e)
    {
        InsertText(e.Text);
    }

    public void HandleKeyDown(Key key, KeyModifiers? modifiers = null)
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
                else InsertText(key.ToString());
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
                if (isCtrlPressed) _ = HandlePaste();
                break;
            default:
                if (key >= Key.A && key <= Key.Z) InsertText(key.ToString());
                break;
        }
    }

    private void HandleBackspace(bool isCtrlPressed)
    {
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
                    var text = _textBufferService.GetText();
                    var wordStart = _textAnalysisService.GetWordBoundariesAt(text, cursorPosition - 1).start;
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
        var (selectionStart, selectionLength) = _selectionService.GetSelection();
        if (selectionLength != 0)
        {
            DeleteSelectedText();
        }
        else
        {
            var cursorPosition = _cursorService.GetCursorPosition();
            if (cursorPosition < _textBufferService.Length)
            {
                if (isCtrlPressed)
                {
                    var text = _textBufferService.GetText();
                    var wordEnd = _textAnalysisService.GetWordBoundariesAt(text, cursorPosition).end;
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
        var text = _textBufferService.GetText();
        int newPosition;

        if (isCtrlPressed)
            newPosition = key switch
            {
                Key.Left => _textAnalysisService.GetWordBoundariesAt(text, currentPosition).start,
                Key.Right => _textAnalysisService.GetWordBoundariesAt(text, currentPosition).end,
                Key.Up => 0,
                Key.Down => text.Length,
                _ => currentPosition
            };
        else
            newPosition = GetNewCursorPosition(key, currentPosition);

        if (newPosition != currentPosition)
        {
            if (isShiftPressed)
            {
                var (selectionStart, _) = _selectionService.GetSelection();
                if (selectionStart == currentPosition) _selectionService.StartSelection(currentPosition);
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
        var textLength = _textBufferService.Length;
        _selectionService.SetSelection(0, textLength);
        _cursorService.SetCursorPosition(textLength);
    }


    private void HandleHomeEnd(Key key, bool isShiftPressed, bool isCtrlPressed)
    {
        var text = _textBufferService.GetText();
        var currentPosition = _cursorService.GetCursorPosition();
        int newPosition;

        if (key == Key.Home)
        {
            if (isCtrlPressed)
                newPosition = _textAnalysisService.GetLineBoundariesAt(text, currentPosition).start;
            else
                newPosition = 0;
        }
        else // Key.End
        {
            if (isCtrlPressed)
                newPosition = _textAnalysisService.GetLineBoundariesAt(text, currentPosition).end;
            else
                newPosition = text.Length;
        }

        if (isShiftPressed)
            _selectionService.UpdateSelection(newPosition);
        else
            _selectionService.ClearSelection();

        _cursorService.SetCursorPosition(newPosition);
    }

    private void HandlePageUpDown(Key key, bool isShiftPressed)
    {
        // TODO Implement page up/down movement based on the number of lines in the viewport
        const int linesToMove = 10;
        var currentPosition = _cursorService.GetCursorPosition();
        var text = _textBufferService.GetText();
        var newPosition = currentPosition;

        for (var i = 0; i < linesToMove; i++)
            newPosition = key == Key.PageUp
                ? _textAnalysisService.GetPositionAbove(text, newPosition)
                : _textAnalysisService.GetPositionBelow(text, newPosition);

        if (isShiftPressed)
            _selectionService.UpdateSelection(newPosition);
        else
            _selectionService.ClearSelection();

        _cursorService.SetCursorPosition(newPosition);
    }

    private void HandleTab(bool isShiftPressed)
    {
        if (isShiftPressed)
        {
            // Unindent
            var (selectionStart, selectionLength) = _selectionService.GetSelection();
            var text = _textBufferService.GetText();
            var (lineStart, lineEnd) = _textAnalysisService.GetLineBoundariesAt(text, selectionStart);

            if (text[lineStart] == '\t')
            {
                DeleteText(lineStart, 1);
                _selectionService.SetSelection(selectionStart - 1, selectionLength - 1);
            }
        }
        else
        {
            // Indent
            InsertText("\t");
        }
    }

    private void HandleCopy()
    {
        var (selectionStart, selectionLength) = _selectionService.GetSelection();
        if (selectionLength != 0)
        {
            var selectedText = _textBufferService.GetText(selectionStart, Math.Abs(selectionLength));
            _clipboardService.SetText(selectedText);
        }
    }

    private void HandleCut()
    {
        HandleCopy();
        var (selectionStart, selectionLength) = _selectionService.GetSelection();
        if (selectionLength != 0)
        {
            DeleteText(selectionStart, Math.Abs(selectionLength));
            _cursorService.SetCursorPosition(selectionStart);
            _selectionService.ClearSelection();
        }
    }

    private async Task HandlePaste()
    {
        var textToPaste = await _clipboardService.GetText();
        if (!string.IsNullOrEmpty(textToPaste)) InsertText(textToPaste);
    }

    public int GetNewCursorPosition(Key key, int currentPosition)
    {
        var text = _textBufferService.GetText();
        return key switch
        {
            Key.Left => Math.Max(0, currentPosition - 1),
            Key.Right => Math.Min(text.Length, currentPosition + 1),
            Key.Up => GetVerticalPosition(text, currentPosition, true),
            Key.Down => GetVerticalPosition(text, currentPosition, false),
            _ => currentPosition
        };
    }

    private int GetVerticalPosition(string text, int currentPosition, bool isUp)
    {
        var (lineStart, lineEnd) = _textAnalysisService.GetLineBoundariesAt(text, currentPosition);
        var columnInLine = currentPosition - lineStart;

        if (isUp)
        {
            if (lineStart == 0) return currentPosition;
            var prevLineEnd = text.LastIndexOf('\n', lineStart - 2);
            var prevLineStart = prevLineEnd == -1 ? 0 : prevLineEnd + 1;
            var prevLineLength = lineStart - prevLineStart - 1;
            return prevLineStart + Math.Min(columnInLine, prevLineLength);
        }

        if (lineEnd == text.Length) return currentPosition;
        var nextLineStart = lineEnd + 1;
        var nextLineEnd = text.IndexOf('\n', nextLineStart);
        nextLineEnd = nextLineEnd == -1 ? text.Length : nextLineEnd;
        var nextLineLength = nextLineEnd - nextLineStart;
        return nextLineStart + Math.Min(columnInLine, nextLineLength);
    }

    public void HandlePointerPressed(PointerPressedEventArgs e)
    {
        int index;
        if (e.X != 0 || e.Y != 0)
        {
            var text = _textBufferService.GetText();
            index = ClampIndex(_textMeasurer.GetIndexAtPosition(text, e.X, e.Y));
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

        var text = _textBufferService.GetText();
        var index = ClampIndex(_textMeasurer.GetIndexAtPosition(text, e.X, e.Y));

        if (e.IsLeftButtonPressed)
        {
            UpdateSelectionAndCursor(index);
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
        var text = _textBufferService.GetText();
        var (start, end) = _textAnalysisService.GetWordBoundariesAt(text, index);
        SetSelectionAndCursor(start, end);
    }

    private void HandleTripleClick(int index)
    {
        var text = _textBufferService.GetText();
        var (start, end) = _textAnalysisService.GetLineBoundariesAt(text, index);
        SetSelectionAndCursor(start, end);
    }

    private void UpdateSelectionAndCursor(int index)
    {
        if (_selectionStart == -1) _selectionStart = index;

        _cursorService.SetCursorPosition(index);
        _selectionService.SetSelection(Math.Min(_selectionStart, index), Math.Abs(_selectionStart - index));
    }

    private void FinalizeSelection(int index)
    {
        var (start, length) = _selectionService.GetSelection();
        if (length == 0 && index == start)
        {
            _selectionService.ClearSelection();
        }
        else
        {
            var selectionStart = Math.Min(start, index);
            var selectionEnd = Math.Max(start, index);
            _selectionService.SetSelection(selectionStart, selectionEnd - selectionStart);
        }

        _cursorService.SetCursorPosition(index);
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
        return Math.Clamp(index, 0, _textBufferService.Length);
    }

    private void DeleteSelectedText()
    {
        var (selectionStart, selectionLength) = _selectionService.GetSelection();
        if (selectionLength != 0)
        {
            selectionStart = Math.Max(0, Math.Min(selectionStart, _textBufferService.Length));
            var endIndex = Math.Max(0, Math.Min(selectionStart + selectionLength, _textBufferService.Length));
            _textBufferService.Delete(selectionStart, endIndex - selectionStart);
            _cursorService.SetCursorPosition(selectionStart);
            _selectionService.ClearSelection();
        }
    }
}