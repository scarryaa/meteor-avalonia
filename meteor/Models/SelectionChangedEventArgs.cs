using System;

namespace meteor.Models;

public class SelectionChangedEventArgs(long? selectionStart = null, long? selectionEnd = null) : EventArgs
{
    public long? SelectionStart { get; } = selectionStart;
    public long? SelectionEnd { get; } = selectionEnd;
}