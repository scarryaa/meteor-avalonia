namespace meteor.Core.Models;

public class HighlightedSegment
{
    public string Text { get; }
    public string Style { get; }

    public HighlightedSegment(string text, string style)
    {
        Text = text;
        Style = style;
    }
}