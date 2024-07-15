namespace meteor.Core.Models.Events;

public class CursorPositionChangedEventArgs(int position, List<int> lineStarts, int lastLineLength) : EventArgs
{
    public int Position { get; } = position;
    public List<int> LineStarts { get; } = lineStarts;
    public int LastLineLength { get; } = lastLineLength;
}