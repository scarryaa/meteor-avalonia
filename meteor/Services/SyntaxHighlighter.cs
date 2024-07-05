using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using meteor.Enums;
using meteor.Interfaces;
using meteor.Models;

namespace meteor.Services;

public class SyntaxHighlighter : ISyntaxHighlighter
{
    private static readonly Regex KeywordRegex = new(
        @"\b(abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|virtual|void|volatile|while)\b",
        RegexOptions.Compiled);

    private static readonly Regex CommentRegex = new(
        @"//.*?$|/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex StringRegex = new(@"""(?:\\.|[^\\""])*""", RegexOptions.Compiled);

    private static readonly Regex TypeRegex = new(@"\b(int|string|bool|float|double|char|void|object|var)\b",
        RegexOptions.Compiled);

    private static readonly Regex NumberRegex = new(@"\b\d+(\.\d+)?\b", RegexOptions.Compiled);

    public List<SyntaxToken> HighlightSyntax(string text)
    {
        return HighlightSyntaxInternal(text, 0, text.Split('\n').Length - 1);
    }

    public List<SyntaxToken> HighlightSyntax(string text, int startLine, int endLine)
    {
        return HighlightSyntaxInternal(text, startLine, endLine);
    }

    private List<SyntaxToken> HighlightSyntaxInternal(string text, int startLine, int endLine)
    {
        var tokens = new List<SyntaxToken>();
        var lines = text.Split('\n');

        for (var lineIndex = startLine; lineIndex <= endLine && lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];

            // Find all matches for each token type
            var keywordMatches = KeywordRegex.Matches(line);
            var commentMatches = CommentRegex.Matches(line);
            var stringMatches = StringRegex.Matches(line);
            var typeMatches = TypeRegex.Matches(line);
            var numberMatches = NumberRegex.Matches(line);

            // Add tokens for each match
            AddTokensFromMatches(tokens, keywordMatches, lineIndex, SyntaxTokenType.Keyword);
            AddTokensFromMatches(tokens, commentMatches, lineIndex, SyntaxTokenType.Comment);
            AddTokensFromMatches(tokens, stringMatches, lineIndex, SyntaxTokenType.String);
            AddTokensFromMatches(tokens, typeMatches, lineIndex, SyntaxTokenType.Type);
            AddTokensFromMatches(tokens, numberMatches, lineIndex, SyntaxTokenType.Number);
        }

        return tokens;
    }

    private void AddTokensFromMatches(List<SyntaxToken> tokens, MatchCollection matches, int lineIndex,
        SyntaxTokenType tokenType)
    {
        foreach (Match match in matches)
            try
            {
                tokens.Add(new SyntaxToken(lineIndex, match.Index, match.Length, tokenType));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding token: {ex.Message}");
            }
    }
}