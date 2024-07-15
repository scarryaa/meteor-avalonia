namespace meteor.Core.Models.Events;

public class TextChangedEventArgs : EventArgs
{
    public int Position { get; }
    public string InsertedText { get; }
    public int DeletedLength { get; }

    public TextChangedEventArgs(int position, string insertedText, int deletedLength)
    {
        Position = position;
        InsertedText = insertedText;
        DeletedLength = deletedLength;
    }
}