using System.Text;
using System.Text.RegularExpressions;
using meteor.Core.Enums.SyntaxHighlighting;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.SyntaxHighlighting;

namespace meteor.Application.Services;

public class SyntaxHighlighter : ISyntaxHighlighter
{
    private static readonly string[] Keywords = { "if", "else", "for", "while", "return" };
    private static readonly Regex KeywordRegex =
        new(@"\b(" + string.Join("|", Keywords) + @")\b", RegexOptions.Compiled);
    private static readonly Regex CommentRegex =
        new(@"//.*?$|/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex StringRegex = new(@"""[^""\\]*(?:\\.[^""\\]*)*""", RegexOptions.Compiled);

    private readonly ITextBufferService _textBufferService;
    private readonly StringBuilder _stringBuilder = new();

    public SyntaxHighlighter(ITextBufferService textBufferService)
    {
        _textBufferService = textBufferService;
    }

    public IEnumerable<SyntaxHighlightingResult> Highlight(string text)
    {
        var results = new List<SyntaxHighlightingResult>();
        _stringBuilder.Clear();
        _textBufferService.AppendTo(_stringBuilder);
        var fullText = _stringBuilder.ToString();

        HighlightKeywords(fullText, results);
        HighlightComments(fullText, results);
        HighlightStrings(fullText, results);
        results.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
        return results;
    }

    private void HighlightKeywords(string text, List<SyntaxHighlightingResult> results)
    {
        foreach (Match match in KeywordRegex.Matches(text))
            results.Add(new SyntaxHighlightingResult
            {
                StartIndex = match.Index,
                Length = match.Length,
                Type = SyntaxHighlightingType.Keyword
            });
    }

    private void HighlightComments(string text, List<SyntaxHighlightingResult> results)
    {
        foreach (Match match in CommentRegex.Matches(text))
            results.Add(new SyntaxHighlightingResult
            {
                StartIndex = match.Index,
                Length = match.Length,
                Type = SyntaxHighlightingType.Comment
            });
    }

    private void HighlightStrings(string text, List<SyntaxHighlightingResult> results)
    {
        foreach (Match match in StringRegex.Matches(text))
            results.Add(new SyntaxHighlightingResult
            {
                StartIndex = match.Index,
                Length = match.Length,
                Type = SyntaxHighlightingType.String
            });
    }
}
