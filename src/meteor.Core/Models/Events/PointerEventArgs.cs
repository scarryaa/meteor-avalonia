namespace meteor.Core.Models.Events;

public class PointerEventArgs
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Index { get; init; }

    public PointerEventArgs()
    {
    }

    public PointerEventArgs(int x, int y, int index)
    {
        X = x;
        Y = y;
        Index = index;
    }
}