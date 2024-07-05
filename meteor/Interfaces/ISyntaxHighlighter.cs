using System.Collections.Generic;
using meteor.Models;

namespace meteor.Interfaces;

public interface ISyntaxHighlighter
{
    List<SyntaxToken> HighlightSyntax(string text);
    List<SyntaxToken> HighlightSyntax(string text, int startLine, int endLine);
}