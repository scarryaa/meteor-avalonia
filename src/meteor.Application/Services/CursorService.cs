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
        int lineStart = 0;
        int lineCount = 0;
        int index = 0;

        while (index < _textBufferService.Length && lineCount < y)
        {
            if (_textBufferService[index] == '\n')
            {
                lineCount++;
                lineStart = index + 1;
            }
            index++;
        }

        index = lineStart;
        for (int i = 0; i < x && index < _textBufferService.Length; i++)
        {
            if (_textBufferService[index] == '\n')
            {
                break;
            }
            index++;
        }

        SetCursorPosition(index);
    }

    public void SetCursorPosition(int index)
    {
        _cursorPosition = Math.Clamp(index, 0, _textBufferService.Length);
    }

    public int GetCursorPosition()
    {
        return _cursorPosition;
    }
}