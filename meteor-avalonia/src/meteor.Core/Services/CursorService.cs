using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class CursorService : ICursorService
{
    private readonly ITabService _tabService;
    private int _cursorPosition;

    public CursorService(ITabService tabService)
    {
        _tabService = tabService;
        _cursorPosition = 0;
    }

    public void MoveCursor(int x, int y)
    {
        var textBufferService = _tabService.GetActiveTextBufferService();
        var lineStart = 0;
        var lineCount = 0;
        var index = 0;

        while (index < textBufferService.Length && lineCount < y)
        {
            if (textBufferService[index] == '\n')
            {
                lineCount++;
                lineStart = index + 1;
            }

            index++;
        }

        index = lineStart;
        for (var i = 0; i < x && index < textBufferService.Length; i++)
        {
            if (textBufferService[index] == '\n') break;
            index++;
        }

        SetCursorPosition(index);
    }

    public void SetCursorPosition(int index)
    {
        _cursorPosition = Math.Clamp(index, 0, _tabService.GetActiveTextBufferService().Length);
    }

    public int GetCursorPosition()
    {
        return _cursorPosition;
    }
}