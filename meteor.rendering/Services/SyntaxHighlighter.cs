using System.Text.RegularExpressions;
using meteor.core.Models;
using meteor.rendering.Models;

namespace meteor.rendering.Services;

public class SyntaxHighlighter
{
    private static readonly Dictionary<string, IBrush> Brushes = new()
    {
        { "keyword", new AvaloniaBrushWrapper(Avalonia.Media.Brushes.Blue) },
        { "string", new AvaloniaBrushWrapper(Avalonia.Media.Brushes.DarkRed) },
        { "comment", new AvaloniaBrushWrapper(Avalonia.Media.Brushes.Green) },
        { "number", new AvaloniaBrushWrapper(Avalonia.Media.Brushes.DarkOrange) },
        { "default", new AvaloniaBrushWrapper(Avalonia.Media.Brushes.Black) }
    };

    private static readonly List<(string type, string pattern)> Rules = new()
    {
        ("keyword",
            @"\b(abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|virtual|void|volatile|while)\b"),
        ("string", @"@?[""'].*?[""']"),
        ("comment", @"//.*?$|/\*.*?\*/"),
        ("number", @"\b\d+(\.\d+)?\b")
    };

    public IReadOnlyList<HighlightedSegment> Highlight(string text)
    {
        var segments = new List<HighlightedSegment>();
        var lastIndex = 0;

        foreach (var (type, pattern) in Rules)
        foreach (Match match in Regex.Matches(text, pattern, RegexOptions.Multiline))
        {
            if (match.Index > lastIndex)
                segments.Add(new HighlightedSegment(text.Substring(lastIndex, match.Index - lastIndex),
                    Brushes["default"]));

            segments.Add(new HighlightedSegment(match.Value, Brushes[type]));
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
            segments.Add(new HighlightedSegment(text.Substring(lastIndex), Brushes["default"]));

        return segments;
    }
}