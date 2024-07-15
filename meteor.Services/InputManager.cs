using meteor.Core.Enums;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Interfaces.Events;
using meteor.Core.Interfaces.Rendering;

namespace meteor.Services;

public class InputManager : IInputManager
{
    private readonly ICursorManager _cursorManager;
    private readonly ISelectionHandler _selectionHandler;
    private readonly ITextEditorCommands _editorCommands;
    private IPoint _lastClickPosition;
    private DateTime _lastClickTime;

    private const int DoubleClickTimeThreshold = 300;
    private const int TripleClickTimeThreshold = 600;
    private const double DoubleClickDistanceThreshold = 5;

    public bool IsTripleClickDrag { get; private set; }
    public bool IsDoubleClickDrag { get; private set; }

    public InputManager(ICursorManager cursorManager, ISelectionHandler selectionHandler,
        ITextEditorCommands editorCommands)
    {
        _cursorManager = cursorManager;
        _selectionHandler = selectionHandler;
        _editorCommands = editorCommands;
    }

    public void OnPointerPressed(IPointerPressedEventArgs e)
    {
        var currentPosition = e.GetPosition();
        var currentTime = DateTime.Now;
        var textPosition = _editorCommands.GetPositionFromPoint(currentPosition);

        if (IsTripleClick(currentTime, currentPosition))
        {
            _selectionHandler.SelectLine(textPosition);
            IsTripleClickDrag = true;
        }
        else if (IsDoubleClick(currentTime, currentPosition))
        {
            _selectionHandler.SelectWord(textPosition);
            IsDoubleClickDrag = true;
        }
        else
        {
            _cursorManager.SetPosition(textPosition);
            _selectionHandler.StartSelection(textPosition);
        }

        _lastClickPosition = currentPosition;
        _lastClickTime = currentTime;
        e.Handled = true;
    }

    public void OnPointerMoved(IPointerEventArgs e)
    {
        if (_selectionHandler.IsSelecting || IsDoubleClickDrag || IsTripleClickDrag)
        {
            var position = _editorCommands.GetPositionFromPoint(e.GetPosition());
            _selectionHandler.UpdateSelectionDuringDrag(position, IsDoubleClickDrag, IsTripleClickDrag);
            e.Handled = true;
        }
    }

    public void OnPointerReleased(IPointerReleasedEventArgs e)
    {
        _selectionHandler.EndSelection();
        IsTripleClickDrag = false;
        IsDoubleClickDrag = false;
        e.Handled = true;
    }

    public void OnKeyDown(IKeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                _cursorManager.MoveCursorLeft(e.IsShiftPressed);
                break;
            case Key.Right:
                _cursorManager.MoveCursorRight(e.IsShiftPressed);
                break;
            case Key.Up:
                _cursorManager.MoveCursorUp(e.IsShiftPressed);
                break;
            case Key.Down:
                _cursorManager.MoveCursorDown(e.IsShiftPressed);
                break;
            case Key.Home:
                _cursorManager.MoveCursorToLineStart(e.IsShiftPressed);
                break;
            case Key.End:
                _cursorManager.MoveCursorToLineEnd(e.IsShiftPressed);
                break;
            case Key.Back:
                _editorCommands.HandleBackspace();
                break;
            case Key.Delete:
                _editorCommands.HandleDelete();
                break;
            case Key.Enter:
                _editorCommands.InsertNewLine();
                break;
        }

        if (e.IsControlPressed)
            HandleControlKeyCombo(e);

        e.Handled = true;
    }

    public void OnTextInput(ITextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text))
        {
            _editorCommands.InsertText(_cursorManager.Position, e.Text);
            e.Handled = true;
        }
    }

    private bool IsTripleClick(DateTime currentTime, IPoint currentPosition)
    {
        return (currentTime - _lastClickTime).TotalMilliseconds <= TripleClickTimeThreshold &&
               DistanceBetweenPoints(currentPosition, _lastClickPosition) <= DoubleClickDistanceThreshold &&
               (currentTime - _lastClickTime).TotalMilliseconds > DoubleClickTimeThreshold;
    }

    private bool IsDoubleClick(DateTime currentTime, IPoint currentPosition)
    {
        return (currentTime - _lastClickTime).TotalMilliseconds <= DoubleClickTimeThreshold &&
               DistanceBetweenPoints(currentPosition, _lastClickPosition) <= DoubleClickDistanceThreshold;
    }

    private void HandleControlKeyCombo(IKeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.C:
                _editorCommands.CopyText();
                break;
            case Key.V:
                _editorCommands.PasteText();
                break;
            case Key.X:
                _editorCommands.CutText();
                break;
            case Key.A:
                _selectionHandler.SelectAll();
                break;
            case Key.Z:
                if (e.IsShiftPressed)
                    _editorCommands.Redo();
                else
                    _editorCommands.Undo();
                break;
        }
    }

    private double DistanceBetweenPoints(IPoint p1, IPoint p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}