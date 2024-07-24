namespace meteor.Core.Interfaces.Services;

public interface ICursorManager
{
    int Position { get; }
    void MoveCursor(int offset);
    public (double X, double Y) GetCursorPosition(ITextMeasurer textMeasurer, string text);
}