namespace meteor.Core.Models.Events;

public class PointerPressedEventArgs
{
    public int Index { get; init; }

    public PointerPressedEventArgs()
    {
    }

    public PointerPressedEventArgs(int index)
    {
        Index = index;
    }
}