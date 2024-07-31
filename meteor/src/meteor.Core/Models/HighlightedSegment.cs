namespace meteor.Core.Models;

public class HighlightedSegment
{
    public HighlightedSegment(string text, string style)
    {
        Text = text;
        Style = style;
    }

    public string Text { get; }
    public string Style { get; }
}