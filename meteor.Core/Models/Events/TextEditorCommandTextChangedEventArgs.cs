namespace meteor.Core.Models.Events;

public class TextEditorCommandTextChangedEventArgs : EventArgs
{
    public string NewText { get; }
    public int Position { get; }
    public int Length { get; }

    public TextEditorCommandTextChangedEventArgs(string newText, int position, int length)
    {
        NewText = newText;
        Position = position;
        Length = length;
    }
}