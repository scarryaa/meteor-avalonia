namespace meteor.Models;

public class TextState(string text, int cursorPosition)
{
    public string Text { get; } = text;
    public int CursorPosition { get; } = cursorPosition;
}