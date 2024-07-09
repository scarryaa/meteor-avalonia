using System;

namespace meteor.Views.Models;

public class TextChangedEventArgs : EventArgs
{
    public long Position { get; }
    public string InsertedText { get; }
    public int? DeletedLength { get; }
    public int Version { get; }

    public TextChangedEventArgs(long position, string insertedText, long deletedLength, int version)
    {
        Position = position;
        InsertedText = insertedText;
        DeletedLength = (int)deletedLength;
        Version = version;
    }
}