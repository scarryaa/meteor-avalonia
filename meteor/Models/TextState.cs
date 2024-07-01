namespace meteor.Models;

public class TextState
{
    public string Text { get; }
    public int CursorPosition { get; }

    public TextState(string text, int cursorPosition)
    {
        Text = text;
        CursorPosition = cursorPosition;
    }
}