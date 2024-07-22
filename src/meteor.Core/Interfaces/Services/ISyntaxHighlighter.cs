using meteor.Core.Models.SyntaxHighlighting;
using meteor.Core.Models.Text;

namespace meteor.Core.Interfaces.Services;

public interface ISyntaxHighlighter
{
    IEnumerable<SyntaxHighlightingResult> Highlight(string? text, TextChangeInfo changeInfo = null);
}