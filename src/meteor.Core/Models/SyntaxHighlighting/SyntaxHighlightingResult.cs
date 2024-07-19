using meteor.Core.Enums.SyntaxHighlighting;

namespace meteor.Core.Models.SyntaxHighlighting;

public class SyntaxHighlightingResult
{
    public SyntaxHighlightingResult()
    {
    }

    public SyntaxHighlightingResult(int startIndex, int length, SyntaxHighlightingType type)
    {
        StartIndex = startIndex;
        Length = length;
        Type = type;
    }

    public int StartIndex { get; set; }
    public int Length { get; set; }
    public SyntaxHighlightingType Type { get; set; }
}