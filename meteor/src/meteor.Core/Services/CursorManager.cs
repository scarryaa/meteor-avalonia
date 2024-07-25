using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class CursorManager : ICursorManager
{
    private readonly ITextBufferService _textBufferService;
    public int Position { get; private set; }
    private int _line;
    private int _column;
    private int _lastKnownLineStart;

    public CursorManager(ITextBufferService textBufferService)
    {
        _textBufferService = textBufferService;
        _line = 0;
        _column = 0;
        _lastKnownLineStart = 0;
    }

    public void MoveCursor(int offset)
    {
        var newPosition = Position + offset;
        var contentLength = _textBufferService.GetLength();
        Position = Math.Clamp(newPosition, 0, contentLength);
        UpdateLineAndColumn();
    }

    public (double X, double Y) GetCursorPosition(ITextMeasurer textMeasurer, string text)
    {
        var lineText = GetCurrentLineText();
        var size = textMeasurer.MeasureText(lineText.Substring(0, _column), "Consolas", 13);
        return (size.Width, _line * textMeasurer.GetLineHeight("Consolas", 13));
    }

    public int GetCursorLine()
    {
        return _line;
    }

    public int GetCursorColumn()
    {
        return _column;
    }

    private void UpdateLineAndColumn()
    {
        var content = _textBufferService.GetContent();
        if (Position < _lastKnownLineStart)
        {
            _line = 0;
            _column = 0;
            _lastKnownLineStart = 0;
        }

        for (var i = _lastKnownLineStart; i < Position; i++)
            if (content[i] == '\n')
            {
                _line++;
                _column = 0;
                _lastKnownLineStart = i + 1;
            }
            else
            {
                _column++;
            }
    }

    private string GetCurrentLineText()
    {
        var content = _textBufferService.GetContent();
        var lineStart = content.LastIndexOf('\n', Position - 1) + 1;
        var lineEnd = content.IndexOf('\n', Position);
        if (lineEnd == -1) lineEnd = content.Length;
        return content.Substring(lineStart, lineEnd - lineStart);
    }
}