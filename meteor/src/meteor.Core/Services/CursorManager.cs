using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class CursorManager : ICursorManager
{
    private readonly ITextBufferService _textBufferService;
    public int Position { get; private set; }

    public CursorManager(ITextBufferService textBufferService)
    {
        _textBufferService = textBufferService;
    }

    public void MoveCursor(int offset)
    {
        var newPosition = Position + offset;
        var contentLength = _textBufferService.GetLength();
        Position = Math.Clamp(newPosition, 0, contentLength);
    }

    public (double X, double Y) GetCursorPosition(ITextMeasurer textMeasurer, string text)
    {
        var textUpToCursor = text.Substring(0, Math.Min(Position, text.Length));
        var size = textMeasurer.Measure(textUpToCursor);
        return (size.Width, 0);
    }
}