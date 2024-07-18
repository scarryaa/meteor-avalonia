using meteor.Core.Models.SyntaxHighlighting;

namespace meteor.Core.Interfaces.Services;

public interface ISyntaxHighlighter
{
    IEnumerable<SyntaxHighlightingResult> Highlight(string text);
}