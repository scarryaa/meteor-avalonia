namespace meteor.Core.Models.Events;

public class PointerReleasedEventArgs
{
    public int Index { get; set; }

    public PointerReleasedEventArgs()
    {
    }

    public PointerReleasedEventArgs(int index)
    {
        Index = index;
    }
}