using meteor.Core.Models;

namespace meteor.Core.Interfaces;

public interface ISyntaxHighlighter
{
    IEnumerable<SyntaxToken> HighlightSyntax(string text, string filePath);
    IEnumerable<SyntaxToken> HighlightSyntax(string text, int startLine, int endLine, string filePath);
}