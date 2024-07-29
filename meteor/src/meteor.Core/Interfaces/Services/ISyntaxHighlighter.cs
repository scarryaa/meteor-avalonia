using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services;

public interface ISyntaxHighlighter
{
    List<HighlightedSegment> HighlightSyntax(string text, string language);
}