namespace meteor.Core.Models.Events;

public class IsSelectingChangedEventArgs(bool isSelecting)
{
    public bool IsSelecting { get; } = isSelecting;
}