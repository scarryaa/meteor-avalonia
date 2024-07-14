using System;
using Avalonia.Input;
using meteor.core.Models;
using meteor.core.Services;

namespace meteor.avalonia.Services;

public class InputManager
{
    private readonly TextBuffer _textBuffer;
    private readonly CursorManager _cursorManager;
    private readonly SelectionManager _selectionManager;

    public InputManager(TextBuffer textBuffer, CursorManager cursorManager, SelectionManager selectionManager)
    {
        _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
        _cursorManager = cursorManager ?? throw new ArgumentNullException(nameof(cursorManager));
        _selectionManager = selectionManager ?? throw new ArgumentNullException(nameof(selectionManager));
    }

    public void HandleTextInput(string text)
    {
        _textBuffer.InsertTextAtCursor(text);
    }

    public void HandleKeyPress(KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control)
            HandleControlKeyPress(e.Key);
        else
            switch (e.Key)
            {
                case Key.Enter:
                    _textBuffer.InsertTextAtCursor(Environment.NewLine);
                    break;
                case Key.Back:
                    _textBuffer.DeleteTextAtCursor(-1);
                    break;
                case Key.Delete:
                    _textBuffer.DeleteTextAtCursor(1);
                    break;
                case Key.Left:
                    _cursorManager.MoveCursorLeft();
                    break;
                case Key.Right:
                    _cursorManager.MoveCursorRight();
                    break;
                case Key.Up:
                    _cursorManager.MoveCursorUp();
                    break;
                case Key.Down:
                    _cursorManager.MoveCursorDown();
                    break;
                default:
                    HandleSpecialKeyPress(e.Key);
                    break;
            }
    }

    private void HandleControlKeyPress(Key key)
    {
        switch (key)
        {
            case Key.Left:
                _cursorManager.MoveCursorWordLeft();
                break;
            case Key.Right:
                _cursorManager.MoveCursorWordRight();
                break;
            case Key.Up:
                _cursorManager.MoveCursorToStartOfDocument();
                break;
            case Key.Down:
                _cursorManager.MoveCursorToEndOfDocument();
                break;
        }
    }

    private void HandleSpecialKeyPress(Key key)
    {
        // Add any special key handling here if needed
    }
}