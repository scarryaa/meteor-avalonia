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
    private DateTime _lastClickTime = DateTime.MinValue;
    private int _clickCount;

    public InputService(
        ITextBufferService textBufferService,
        ICursorService cursorService,
        ITextAnalysisService textAnalysisService,
        ISelectionService selectionService)
    {
        _textBufferService = textBufferService;
        _cursorService = cursorService;
        _textAnalysisService = textAnalysisService;
        _selectionService = selectionService;
    }

    public void InsertText(string text)
    {
        var cursorPosition = _cursorService.GetCursorPosition();
        cursorPosition = Math.Clamp(cursorPosition, 0, _textBufferService.Length);
        _textBufferService.Insert(cursorPosition, text);
        _cursorService.SetCursorPosition(cursorPosition + text.Length);
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
        var cursorPosition = _cursorService.GetCursorPosition();
        var isShiftPressed = modifiers.HasValue && modifiers.Value.HasFlag(KeyModifiers.Shift);

        switch (key)
        {
            case Key.Backspace:
                if (cursorPosition > 0)
                {
                    _textBufferService.Delete(cursorPosition - 1, 1);
                    _cursorService.SetCursorPosition(cursorPosition - 1);
                }

                break;
            case Key.Delete:
                if (cursorPosition < _textBufferService.Length) _textBufferService.Delete(cursorPosition, 1);
                break;
            case Key.Enter:
                InsertText("\n");
                break;
            case Key.Left:
            case Key.Right:
            case Key.Up:
            case Key.Down:
                var newPosition = GetNewCursorPosition(key, cursorPosition);
                if (newPosition != cursorPosition)
                {
                    if (isShiftPressed)
                        _selectionService.UpdateSelection(newPosition);
                    else
                        _selectionService.ClearSelection();
                    _cursorService.SetCursorPosition(newPosition);
                }

                break;
            case Key.End:
                _cursorService.SetCursorPosition(_textBufferService.Length);
                break;
            case Key.Home:
                _cursorService.SetCursorPosition(0);
                break;
            default:
                if (key >= Key.A && key <= Key.Z) InsertText(key.ToString());
                break;
        }
    }

    public int GetNewCursorPosition(Key key, int currentPosition)
    {
        var text = _textBufferService.GetText();
        switch (key)
        {
            case Key.Left:
                return Math.Max(0, currentPosition - 1);
            case Key.Right:
                return Math.Min(text.Length, currentPosition + 1);
            case Key.Up:
                return _textAnalysisService.GetPositionAbove(text, currentPosition);
            case Key.Down:
                return _textAnalysisService.GetPositionBelow(text, currentPosition);
            default:
                return currentPosition;
        }
    }

    public void HandlePointerPressed(PointerPressedEventArgs e)
    {
        if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            _clickCount++;
        else
            _clickCount = 1;
        _lastClickTime = DateTime.Now;

        switch (_clickCount)
        {
            case 1:
                HandleSingleClick(e);
                break;
            case 2:
                HandleDoubleClick(e);
                break;
            case 3:
                HandleTripleClick(e);
                _clickCount = 0;
                break;
        }
    }

    public void HandlePointerMoved(PointerEventArgs e)
    {
        _cursorService.MoveCursor(e.X, e.Y);
        _selectionService.UpdateSelection(e.Index);
    }

    public void HandlePointerReleased(PointerReleasedEventArgs e)
    {
        _selectionService.UpdateSelection(e.Index);
    }

    private void HandleSingleClick(PointerPressedEventArgs e)
    {
        _cursorService.SetCursorPosition(e.Index);
        _selectionService.StartSelection(e.Index);
    }

    private void HandleDoubleClick(PointerPressedEventArgs e)
    {
        var index = e.Index;
        var text = _textBufferService.GetText();
        var (start, end) = _textAnalysisService.GetWordBoundariesAt(text, index);
        _selectionService.SetSelection(start, end - start);
        _cursorService.SetCursorPosition(end);
    }

    private void HandleTripleClick(PointerPressedEventArgs e)
    {
        var index = e.Index;
        var text = _textBufferService.GetText();
        var (start, end) = _textAnalysisService.GetLineBoundariesAt(text, index);
        _selectionService.SetSelection(start, end - start);
        _cursorService.SetCursorPosition(end);
    }
}