using System.Collections.Generic;
using System.Text.RegularExpressions;
using meteor.Enums;

namespace meteor.Models;

public class PythonLanguageDefinition : LanguageDefinition
{
    public PythonLanguageDefinition()
    {
        Keywords = new Dictionary<string, SyntaxTokenType>
        {
            { "and", SyntaxTokenType.Keyword },
            { "as", SyntaxTokenType.Keyword },
            { "assert", SyntaxTokenType.Keyword },
            { "break", SyntaxTokenType.Keyword },
            { "class", SyntaxTokenType.Keyword },
            { "continue", SyntaxTokenType.Keyword },
            { "def", SyntaxTokenType.Keyword },
            { "del", SyntaxTokenType.Keyword },
            { "elif", SyntaxTokenType.Keyword },
            { "else", SyntaxTokenType.Keyword },
            { "except", SyntaxTokenType.Keyword },
            { "False", SyntaxTokenType.Keyword },
            { "finally", SyntaxTokenType.Keyword },
            { "for", SyntaxTokenType.Keyword },
            { "from", SyntaxTokenType.Keyword },
            { "global", SyntaxTokenType.Keyword },
            { "if", SyntaxTokenType.Keyword },
            { "import", SyntaxTokenType.Keyword },
            { "in", SyntaxTokenType.Keyword },
            { "is", SyntaxTokenType.Keyword },
            { "lambda", SyntaxTokenType.Keyword },
            { "None", SyntaxTokenType.Keyword },
            { "nonlocal", SyntaxTokenType.Keyword },
            { "not", SyntaxTokenType.Keyword },
            { "or", SyntaxTokenType.Keyword },
            { "pass", SyntaxTokenType.Keyword },
            { "raise", SyntaxTokenType.Keyword },
            { "return", SyntaxTokenType.Keyword },
            { "True", SyntaxTokenType.Keyword },
            { "try", SyntaxTokenType.Keyword },
            { "while", SyntaxTokenType.Keyword },
            { "with", SyntaxTokenType.Keyword },
            { "yield", SyntaxTokenType.Keyword }
        };

        TokenRegex = new Regex(
            @"\b(?<keyword>" + string.Join("|", Keywords.Keys) + @")\b|" +
            @"(?<comment>#.*?$)|" +
            @"(?<string>""""""[\s\S]*?""""""|'''[\s\S]*?'''|"".*?""|'.*?')|" +
            @"(?<number>\b\d+(\.\d+)?\b)|" +
            @"(?<identifier>\b[a-zA-Z_][a-zA-Z0-9_]*\b)",
            RegexOptions.Compiled | RegexOptions.Multiline
        );
    }

    public override List<SyntaxToken> TokenizeQuickly(string line, int lineIndex)
    {
        var tokens = new List<SyntaxToken>();
        var matches = TokenRegex.Matches(line);

        foreach (Match match in matches)
            if (match.Groups["keyword"].Success)
                tokens.Add(new SyntaxToken(lineIndex, match.Index, match.Length, Keywords[match.Value]));
            else if (match.Groups["comment"].Success)
                tokens.Add(new SyntaxToken(lineIndex, match.Index, match.Length, SyntaxTokenType.Comment));
            else if (match.Groups["string"].Success)
                tokens.Add(new SyntaxToken(lineIndex, match.Index, match.Length, SyntaxTokenType.String));
            else if (match.Groups["number"].Success)
                tokens.Add(new SyntaxToken(lineIndex, match.Index, match.Length, SyntaxTokenType.Number));
            else if (match.Groups["identifier"].Success)
                tokens.Add(new SyntaxToken(lineIndex, match.Index, match.Length, SyntaxTokenType.Identifier));

        return tokens;
    }
}