using System.Collections.Generic;
using meteor.Models;

namespace meteor.Interfaces;

public interface ISyntaxHighlighter
{
    List<SyntaxToken> HighlightSyntax(string text, string filePath);
    List<SyntaxToken> HighlightSyntax(string text, int startLine, int endLine, string filePath);
}