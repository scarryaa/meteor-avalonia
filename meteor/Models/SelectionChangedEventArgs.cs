using System;

namespace meteor.Models;

public class SelectionChangedEventArgs : EventArgs
{
    public long? SelectionStart { get; }
    public long? SelectionEnd { get; }

    public SelectionChangedEventArgs(long? selectionStart = null, long? selectionEnd = null)
    {
        SelectionStart = selectionStart;
        SelectionEnd = selectionEnd;
    }
}