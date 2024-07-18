using meteor.Application.Interfaces;
using meteor.Core.Interfaces.Services;

namespace meteor.Application.Services;

public class CursorService : ICursorService
{
    private int _cursorPosition;
    private readonly ITextBufferService _textBufferService;

    public CursorService(ITextBufferService textBufferService)
    {
        _textBufferService = textBufferService;
        _cursorPosition = 0;
    }

    public void MoveCursor(int x, int y)
    {
        var text = _textBufferService.GetText();
        var lines = text.Split('\n');
        var index = 0;

        y = Math.Max(0, Math.Min(y, lines.Length - 1));
        x = Math.Max(0, x);

        for (var i = 0; i < y; i++) index += lines[i].Length + 1; // +1 for newline

        index += Math.Min(x, lines[y].Length);

        SetCursorPosition(index);
    }

    public void SetCursorPosition(int index)
    {
        _cursorPosition = Math.Max(index, 0);
    }

    public int GetCursorPosition()
    {
        return _cursorPosition;
    }
}