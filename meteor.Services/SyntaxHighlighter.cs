using meteor.Core.Interfaces;
using meteor.Core.Models;

namespace meteor.Services;

public class SyntaxHighlighter(Dictionary<string, ILanguageDefinition> languageDefinitions)
    : ISyntaxHighlighter
{
    private readonly Dictionary<string, ILanguageDefinition> _languageDefinitions =
        languageDefinitions ?? throw new ArgumentNullException(nameof(languageDefinitions));

    public IEnumerable<SyntaxToken> HighlightSyntax(string text, string filePath)
    {
        var languageDefinition = DetectLanguage(filePath, text);
        return HighlightSyntaxInternal(text, 0, text.Split('\n').Length - 1, languageDefinition);
    }

    public IEnumerable<SyntaxToken> HighlightSyntax(string text, int startLine, int endLine, string filePath)
    {
        var languageDefinition = DetectLanguage(filePath, text);
        return HighlightSyntaxInternal(text, startLine, endLine, languageDefinition);
    }

    private ILanguageDefinition DetectLanguage(string filePath, string content)
    {
        ILanguageDefinition languageDefinition = null;

        if (!string.IsNullOrEmpty(filePath))
        {
            var extension = Path.GetExtension(filePath).TrimStart('.').ToLower();
            _languageDefinitions.TryGetValue(extension, out languageDefinition);
        }

        if (languageDefinition == null) languageDefinition = DetectLanguageFromContent(content);

        return languageDefinition ?? _languageDefinitions["cs"]; // Default to C# if no language is detected
    }

    private ILanguageDefinition DetectLanguageFromContent(string content)
    {
        var sampleContent = content.Length > 1000 ? content.Substring(0, 1000) : content;

        foreach (var languageDefinition in _languageDefinitions.Values)
            if (languageDefinition.DetectLanguage(sampleContent))
                return languageDefinition;

        return _languageDefinitions["cs"]; // Default to C# if no language is detected
    }

    private IEnumerable<SyntaxToken> HighlightSyntaxInternal(string text, int startLine, int endLine,
        ILanguageDefinition languageDefinition)
    {
        var tokens = new List<SyntaxToken>();
        var lines = text.Split('\n');

        for (var lineIndex = startLine; lineIndex <= endLine && lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineTokens = languageDefinition.Tokenize(line, lineIndex);
            tokens.AddRange(lineTokens);
        }

        return tokens;
    }
}

public interface ILanguageDefinition
{
    bool DetectLanguage(string sampleContent);
    IEnumerable<SyntaxToken> Tokenize(string line, int lineIndex);
}