namespace meteor.Models;

public class TextState
{
    public string Text { get; }
    public long CursorPosition { get; }

    public TextState(string text, long cursorPosition)
    {
        Text = text;
        CursorPosition = cursorPosition;
    }
}