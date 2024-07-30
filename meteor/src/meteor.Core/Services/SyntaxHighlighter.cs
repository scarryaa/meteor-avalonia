using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class SyntaxHighlighter : ISyntaxHighlighter
{
    private readonly ConcurrentDictionary<string, Lazy<List<SyntaxRule>>> _languageRules;

    public record SyntaxRule(Regex Regex, string Style);

    public SyntaxHighlighter()
    {
        _languageRules = new ConcurrentDictionary<string, Lazy<List<SyntaxRule>>>();
        InitializeCSharpRules();
    }

    private void InitializeCSharpRules()
    {
        _languageRules["csharp"] = new Lazy<List<SyntaxRule>>(() =>
        [
            new(
                new Regex(
                    @"\b(abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|virtual|void|volatile|while|var|dynamic|async|await|yield)\b",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase), "keyword"),

            new(
                new Regex(@"^\s*#(if|else|elif|endif|define|undef|warning|error|line|region|endregion|pragma).*?$",
                    RegexOptions.Multiline | RegexOptions.Compiled), "preprocessor"),

            new(new Regex(@"//.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "comment"),
            new(new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled), "comment"),
            new(new Regex(@"///.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "xmldoc"),
            new(new Regex(@"(?<=\[)[^\]]+(?=\])", RegexOptions.Compiled), "attribute"),
            new(new Regex(@"(?<!\w)(\w+)(?=\s*\()", RegexOptions.Compiled), "method"),
            new(new Regex(@"@""(?:[^""]|"""")*""", RegexOptions.Compiled), "string"),
            new(new Regex(@"""(?:\\.|[^\\""])*""", RegexOptions.Compiled), "string"),
            new(new Regex(@"'\\.'|'[^\\]'", RegexOptions.Compiled), "string"),
            new(new Regex(@"\b(0x[a-fA-F0-9]+|0b[01]+|\d+(\.\d+)?([eE][+-]?\d+)?[fFdDmM]?)\b", RegexOptions.Compiled),
                "number"),

            new(new Regex(@"\b([a-zA-Z]\w*\.)+", RegexOptions.Compiled), "namespace"),
            new(new Regex(@"\b[A-Z]\w*\b", RegexOptions.Compiled), "type"),
            new(
                new Regex(@"\b(from|where|select|group|into|orderby|join|let|in|on|equals|by|ascending|descending)\b",
                    RegexOptions.Compiled), "linq"),

            new(new Regex(@"\?\.", RegexOptions.Compiled), "operator"),
            new(new Regex(@"=>\s*{|\S+\s*=>\s*\S+", RegexOptions.Compiled), "lambda")
        ]);
    }

    public List<HighlightedSegment> HighlightSyntax(string text, string language)
    {
        if (!_languageRules.TryGetValue(language, out var lazyRules))
            return [new HighlightedSegment(text, "default")];

        var rules = lazyRules.Value;
        var segments = new List<(int Start, int End, string Style)>();

        // First, find all comment segments
        var commentRules = rules.Where(r => r.Style == "comment" || r.Style == "xmldoc").ToList();
        foreach (var rule in commentRules)
        foreach (Match match in rule.Regex.Matches(text))
            segments.Add((match.Index, match.Index + match.Length, rule.Style));

        // Then, apply other rules only to non-comment parts
        var otherRules = rules.Except(commentRules).ToList();
        var lastIndex = 0;
        foreach (var (start, end, _) in segments.OrderBy(s => s.Start))
        {
            if (start > lastIndex)
                ApplyRules(text.Substring(lastIndex, start - lastIndex), lastIndex, otherRules, segments);
            lastIndex = end;
        }

        if (lastIndex < text.Length) ApplyRules(text.Substring(lastIndex), lastIndex, otherRules, segments);

        segments.Sort((a, b) => a.Start.CompareTo(b.Start));

        var result = new List<HighlightedSegment>();
        lastIndex = 0;

        foreach (var (start, end, style) in segments)
        {
            if (lastIndex < start)
                result.Add(new HighlightedSegment(text.Substring(lastIndex, start - lastIndex), "default"));

            result.Add(new HighlightedSegment(text.Substring(start, end - start), style));
            lastIndex = end;
        }

        if (lastIndex < text.Length)
            result.Add(new HighlightedSegment(text.Substring(lastIndex), "default"));

        return result;
    }

    private void ApplyRules(string text, int offset, List<SyntaxRule> rules,
        List<(int Start, int End, string Style)> segments)
    {
        foreach (var rule in rules)
        foreach (Match match in rule.Regex.Matches(text))
        {
            var start = offset + match.Index;
            var end = start + match.Length;

            if (!segments.Any(s => s.Start < end && start < s.End))
                segments.Add((start, end, rule.Style));
        }
    }

    public void AddLanguageRules(string language, List<SyntaxRule> rules)
    {
        _languageRules[language] = new Lazy<List<SyntaxRule>>(() => rules);
    }
}