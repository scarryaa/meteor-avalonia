using meteor.Enums;

namespace meteor.Models;

public class SyntaxToken
{
    public int Line { get; }
    public int StartColumn { get; }
    public int Length { get; }
    public SyntaxTokenType Type { get; }

    public SyntaxToken(int line, int startColumn, int length, SyntaxTokenType type)
    {
        Line = line;
        StartColumn = startColumn;
        Length = length;
        Type = type;
    }
}