using meteor.Core.Models.SyntaxHighlighting;

namespace meteor.Core.Models.Rendering;

public class RenderLineContext
{
    public int LineStart { get; set; }
    public int LineLength { get; set; }
    public double LineY { get; set; }
    public IEnumerable<SyntaxHighlightingResult> HighlightingResults { get; set; }
    public (int start, int length) Selection { get; set; }
    public int CursorPosition { get; set; }
    public double OffsetX { get; set; }

    public RenderLineContext(int lineStart, int lineLength, double lineY,
        IEnumerable<SyntaxHighlightingResult> highlightingResults,
        (int start, int length) selection, int cursorPosition, double offsetX)
    {
        LineStart = lineStart;
        LineLength = lineLength;
        LineY = lineY;
        HighlightingResults = highlightingResults;
        Selection = selection;
        CursorPosition = cursorPosition;
        OffsetX = offsetX;
    }
}