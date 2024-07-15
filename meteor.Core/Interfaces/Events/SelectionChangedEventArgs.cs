namespace meteor.Core.Interfaces.Events;

public class SelectionChangedEventArgs : EventArgs
{
    public long OldSelectionStart { get; }
    public long OldSelectionEnd { get; }
    public long NewSelectionStart { get; }
    public long NewSelectionEnd { get; }

    public SelectionChangedEventArgs(long oldStart, long oldEnd, long newStart, long newEnd)
    {
        OldSelectionStart = oldStart;
        OldSelectionEnd = oldEnd;
        NewSelectionStart = newStart;
        NewSelectionEnd = newEnd;
    }
}