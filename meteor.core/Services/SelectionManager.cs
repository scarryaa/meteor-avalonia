using meteor.core.Models;

namespace meteor.core.Services;

public class SelectionManager
{
    private readonly TextBuffer _textBuffer;
    private readonly CursorManager _cursorManager;

    public SelectionManager(TextBuffer textBuffer, CursorManager cursorManager)
    {
        _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
        _cursorManager = cursorManager ?? throw new ArgumentNullException(nameof(cursorManager));
    }

    public void HandlePointerPressed(double x, double y, double charWidth, double lineHeight)
    {
        var line = (int)(y / lineHeight);
        var column = (int)(x / charWidth);

        // Ensure the line and column are within valid ranges
        if (line < 0) line = 0;
        if (line >= _textBuffer.LineCount) line = _textBuffer.LineCount - 1;
        if (column < 0) column = 0;
        if (line >= 0 && line < _textBuffer.LineCount && column > _textBuffer.GetLineLength(line))
            column = _textBuffer.GetLineLength(line);

        _cursorManager.SetCursorPosition(line, column);
        _textBuffer.ClearSelection();
    }

    public void HandlePointerMoved(double x, double y, double charWidth, double lineHeight, bool isLeftButtonPressed)
    {
        if (isLeftButtonPressed)
        {
            var line = (int)(y / lineHeight);
            var column = (int)(x / charWidth);

            // Ensure the line and column are within valid ranges
            if (line < 0) line = 0;
            if (line >= _textBuffer.LineCount) line = _textBuffer.LineCount - 1;
            if (column < 0) column = 0;
            if (line >= 0 && line < _textBuffer.LineCount && column > _textBuffer.GetLineLength(line))
                column = _textBuffer.GetLineLength(line);

            _textBuffer.ExtendSelectionTo(line, column);
        }
    }

    public void HandlePointerReleased(double x, double y, double charWidth, double lineHeight)
    {
        var line = (int)(y / lineHeight);
        var column = (int)(x / charWidth);

        // Ensure the line and column are within valid ranges
        if (line < 0) line = 0;
        if (line >= _textBuffer.LineCount) line = _textBuffer.LineCount - 1;
        if (column < 0) column = 0;
        if (line >= 0 && line < _textBuffer.LineCount && column > _textBuffer.GetLineLength(line))
            column = _textBuffer.GetLineLength(line);

        _textBuffer.ExtendSelectionTo(line, column);
    }

    public void HandleKeyPress(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.LeftArrow:
                _cursorManager.MoveCursorLeft();
                _textBuffer.ClearSelection();
                break;
            case ConsoleKey.RightArrow:
                _cursorManager.MoveCursorRight();
                _textBuffer.ClearSelection();
                break;
            case ConsoleKey.UpArrow:
                _cursorManager.MoveCursorUp();
                _textBuffer.ClearSelection();
                break;
            case ConsoleKey.DownArrow:
                _cursorManager.MoveCursorDown();
                _textBuffer.ClearSelection();
                break;
            case ConsoleKey.Backspace:
                _textBuffer.DeleteTextAtCursor(1);
                break;
            case ConsoleKey.Delete:
                _textBuffer.Delete(_textBuffer.CursorPosition.Line, 1);
                break;
            case ConsoleKey.Enter:
                _textBuffer.InsertTextAtCursor("\n");
                break;
        }
    }

    public void HandleTextInput(string text)
    {
        _textBuffer.InsertTextAtCursor(text);
    }
}