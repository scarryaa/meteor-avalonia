namespace meteor.core.Models;

public class CachedLine
{
    public string Text { get; }
    public IReadOnlyList<HighlightedSegment> HighlightedSegments { get; }
    public DateTime Timestamp { get; }

    public CachedLine(string text, IReadOnlyList<HighlightedSegment> highlightedSegments)
    {
        Text = text;
        HighlightedSegments = highlightedSegments;
        Timestamp = DateTime.Now;
    }
}