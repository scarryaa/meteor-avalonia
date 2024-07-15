namespace meteor.Core.Models.Events;

public class CursorPositionChangedEventArgs : EventArgs
{
    public int Position { get; }
    public List<int> LineStarts { get; }
    public int LastLineLength { get; }

    public CursorPositionChangedEventArgs(int position, List<int> lineStarts, int lastLineLength)
    {
        Position = position;
        LineStarts = lineStarts;
        LastLineLength = lastLineLength;
    }
}