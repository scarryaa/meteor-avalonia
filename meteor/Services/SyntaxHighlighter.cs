using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using meteor.Interfaces;
using meteor.Models;

namespace meteor.Services;

public class SyntaxHighlighter : ISyntaxHighlighter
{
    private readonly Dictionary<string, LanguageDefinition> _languageDefinitions = new()
    {
        { "cs", new CSharpLanguageDefinition() },
        { "py", new PythonLanguageDefinition() },
        { "js", new JavaScriptLanguageDefinition() }
    };

    public List<SyntaxToken> HighlightSyntax(string text, string filePath)
    {
        Console.WriteLine($"HighlightSyntax called with filePath: {filePath}");
        var languageDefinition = DetectLanguage(filePath, text);
        return HighlightSyntaxInternal(text, 0, text.Split('\n').Length - 1, languageDefinition);
    }

    public List<SyntaxToken> HighlightSyntax(string text, int startLine, int endLine, string filePath)
    {
        Console.WriteLine(
            $"HighlightSyntax called with startLine: {startLine}, endLine: {endLine}, filePath: {filePath}");
        var languageDefinition = DetectLanguage(filePath, text);
        return HighlightSyntaxInternal(text, startLine, endLine, languageDefinition);
    }

    private LanguageDefinition DetectLanguage(string filePath, string content)
    {
        LanguageDefinition languageDefinition = null;

        if (!string.IsNullOrEmpty(filePath))
        {
            var extension = Path.GetExtension(filePath).TrimStart('.').ToLower();
            _languageDefinitions.TryGetValue(extension, out languageDefinition);
            Console.WriteLine(
                $"File extension detected: {extension}, Language: {languageDefinition?.GetType().Name ?? "None"}");
        }

        if (languageDefinition == null)
        {
            languageDefinition = DetectLanguageFromContent(content);
            Console.WriteLine($"Language detected from content: {languageDefinition?.GetType().Name ?? "None"}");
        }

        return languageDefinition ?? _languageDefinitions["cs"];
    }

    private LanguageDefinition DetectLanguageFromContent(string content)
    {
        var sampleContent = content.Length > 1000 ? content.Substring(0, 1000) : content;

        // C# detection
        if (sampleContent.Contains("using System") ||
            sampleContent.Contains("namespace ") ||
            sampleContent.Contains("class ") ||
            sampleContent.Contains("public ") ||
            sampleContent.Contains("private ") ||
            sampleContent.Contains("protected ") ||
            Regex.IsMatch(sampleContent, @"\bvar\s+\w+\s*="))
        {
            Console.WriteLine("Detected C# from content");
            return _languageDefinitions["cs"];
        }

        // Python detection
        if (sampleContent.Contains("def ") || sampleContent.Contains("import ") ||
            (sampleContent.Contains("class ") && sampleContent.Contains(":")))
        {
            Console.WriteLine("Detected Python from content");
            return _languageDefinitions["py"];
        }

        // JavaScript detection
        if (sampleContent.Contains("function") || sampleContent.Contains("var ") || sampleContent.Contains("let ") ||
            sampleContent.Contains("const "))
        {
            Console.WriteLine("Detected JavaScript from content");
            return _languageDefinitions["js"];
        }

        Console.WriteLine("No specific language detected from content, defaulting to C#");
        return _languageDefinitions["cs"];
    }

    private List<SyntaxToken> HighlightSyntaxInternal(string text, int startLine, int endLine,
        LanguageDefinition languageDefinition)
    {
        var tokens = new List<SyntaxToken>();
        var lines = text.Split('\n');

        for (var lineIndex = startLine; lineIndex <= endLine && lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            Console.WriteLine($"Processing line {lineIndex}: {line}");
            var lineTokens = languageDefinition.TokenizeQuickly(line, lineIndex);
            tokens.AddRange(lineTokens);
            Console.WriteLine($"Generated {lineTokens.Count} tokens for line {lineIndex}");
        }

        Console.WriteLine($"Total tokens generated: {tokens.Count}");
        return tokens;
    }
}