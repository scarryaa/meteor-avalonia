using meteor.Core.Enums;

namespace meteor.Core.Models;

public struct SyntaxToken
{
    public int Line { get; set; }
    public int StartColumn { get; set; }
    public int Length { get; set; }
    public SyntaxTokenType Type { get; set; }
}