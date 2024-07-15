namespace meteor.Core.Models.Events;

public class TextChangedEventArgs(int position, string insertedText, int deletedLength) : EventArgs
{
    public int Position { get; } = position;
    public string InsertedText { get; } = insertedText;
    public int DeletedLength { get; } = deletedLength;
}