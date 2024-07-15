using System;

namespace meteor.Views.Models;

public class TextChangedEventArgs(long position, string insertedText, long deletedLength, int version)
    : EventArgs
{
    public long Position { get; } = position;
    public string InsertedText { get; } = insertedText;
    public int? DeletedLength { get; } = (int)deletedLength;
    public int Version { get; } = version;
}