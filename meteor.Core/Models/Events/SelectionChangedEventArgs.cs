namespace meteor.Core.Models.Events;

public class SelectionChangedEventArgs(int newStart, int newEnd, bool isSelecting)
    : EventArgs
{
    public int NewStart { get; } = newStart;
    public int NewEnd { get; } = newEnd;
    public bool IsSelecting { get; } = isSelecting;
}