namespace meteor.core.Models;

public class HighlightedSegment
{
    public string Text { get; }
    public IBrush Brush { get; }

    public HighlightedSegment(string text, IBrush brush)
    {
        Text = text;
        Brush = brush;
    }
}