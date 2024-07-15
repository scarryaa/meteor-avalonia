using meteor.Enums;

namespace meteor.Models;

public class SyntaxToken(int line, int startColumn, int length, SyntaxTokenType type)
{
    public int Line { get; } = line;
    public int StartColumn { get; } = startColumn;
    public int Length { get; } = length;
    public SyntaxTokenType Type { get; } = type;
}