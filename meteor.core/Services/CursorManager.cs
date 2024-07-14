using meteor.core.Models;

namespace meteor.core.Services;

public class CursorManager
{
    private readonly TextBuffer _textBuffer;

    public CursorManager(TextBuffer textBuffer)
    {
        _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
    }

    public void SetCursorPosition(int line, int column)
    {
        _textBuffer.SetCursorPosition(line, column);
    }

    public void MoveCursorUp()
    {
        var position = _textBuffer.CursorPosition;
        _textBuffer.SetCursorPosition(Math.Max(position.Line - 1, 0), position.Column);
    }

    public void MoveCursorDown()
    {
        var position = _textBuffer.CursorPosition;
        _textBuffer.SetCursorPosition(Math.Min(position.Line + 1, _textBuffer.LineCount - 1), position.Column);
    }

    public void MoveCursorLeft()
    {
        var position = _textBuffer.CursorPosition;
        if (position.Column > 0)
            _textBuffer.SetCursorPosition(position.Line, position.Column - 1);
        else if (position.Line > 0)
            _textBuffer.SetCursorPosition(position.Line - 1, _textBuffer.GetLineLength(position.Line - 1));
    }

    public void MoveCursorRight()
    {
        var position = _textBuffer.CursorPosition;
        if (position.Column < _textBuffer.GetLineLength(position.Line))
            _textBuffer.SetCursorPosition(position.Line, position.Column + 1);
        else if (position.Line < _textBuffer.LineCount - 1) _textBuffer.SetCursorPosition(position.Line + 1, 0);
    }

    public void MoveCursorWordLeft()
    {
        var position = _textBuffer.CursorPosition;
        if (position.Column == 0 && position.Line > 0)
        {
            // Move to the end of the previous line
            _textBuffer.SetCursorPosition(position.Line - 1, _textBuffer.GetLineLength(position.Line - 1));
        }
        else
        {
            var lineText = _textBuffer.GetText(_textBuffer.CalculateCursorPosition(position.Line, 0), position.Column);
            var newColumn = FindPreviousWordBoundary(lineText, position.Column);
            _textBuffer.SetCursorPosition(position.Line, newColumn);
        }
    }

    public void MoveCursorWordRight()
    {
        var position = _textBuffer.CursorPosition;
        var lineLength = _textBuffer.GetLineLength(position.Line);
        if (position.Column == lineLength && position.Line < _textBuffer.LineCount - 1)
        {
            // Move to the start of the next line
            _textBuffer.SetCursorPosition(position.Line + 1, 0);
        }
        else
        {
            var lineText = _textBuffer.GetText(_textBuffer.CalculateCursorPosition(position.Line, 0),
                lineLength - position.Column);
            var newColumn = position.Column + FindNextWordBoundary(lineText, 0);
            _textBuffer.SetCursorPosition(position.Line, newColumn);
        }
    }

    public void MoveCursorToStartOfDocument()
    {
        _textBuffer.SetCursorPosition(0, 0);
    }

    public void MoveCursorToEndOfDocument()
    {
        var lastLine = _textBuffer.LineCount - 1;
        _textBuffer.SetCursorPosition(lastLine, _textBuffer.GetLineLength(lastLine));
    }

    private int FindPreviousWordBoundary(string text, int startIndex)
    {
        if (string.IsNullOrEmpty(text) || startIndex <= 0) return 0;
        var index = startIndex - 1;
        while (index > 0 && !char.IsWhiteSpace(text[index - 1])) index--;
        return index;
    }

    private int FindNextWordBoundary(string text, int startIndex)
    {
        if (string.IsNullOrEmpty(text) || startIndex >= text.Length) return text.Length;
        var index = startIndex;
        while (index < text.Length && !char.IsWhiteSpace(text[index])) index++;
        return index - startIndex;
    }
}