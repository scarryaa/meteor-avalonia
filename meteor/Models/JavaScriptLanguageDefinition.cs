using System.Collections.Generic;
using System.Text.RegularExpressions;
using meteor.Enums;

namespace meteor.Models;

public class JavaScriptLanguageDefinition : LanguageDefinition
{
    public JavaScriptLanguageDefinition()
    {
        Keywords = new Dictionary<string, SyntaxTokenType>
        {
            { "break", SyntaxTokenType.Keyword },
            { "case", SyntaxTokenType.Keyword },
            { "catch", SyntaxTokenType.Keyword },
            { "class", SyntaxTokenType.Keyword },
            { "const", SyntaxTokenType.Keyword },
            { "continue", SyntaxTokenType.Keyword },
            { "debugger", SyntaxTokenType.Keyword },
            { "default", SyntaxTokenType.Keyword },
            { "delete", SyntaxTokenType.Keyword },
            { "do", SyntaxTokenType.Keyword },
            { "else", SyntaxTokenType.Keyword },
            { "export", SyntaxTokenType.Keyword },
            { "extends", SyntaxTokenType.Keyword },
            { "finally", SyntaxTokenType.Keyword },
            { "for", SyntaxTokenType.Keyword },
            { "function", SyntaxTokenType.Keyword },
            { "if", SyntaxTokenType.Keyword },
            { "import", SyntaxTokenType.Keyword },
            { "in", SyntaxTokenType.Keyword },
            { "instanceof", SyntaxTokenType.Keyword },
            { "new", SyntaxTokenType.Keyword },
            { "return", SyntaxTokenType.Keyword },
            { "super", SyntaxTokenType.Keyword },
            { "switch", SyntaxTokenType.Keyword },
            { "this", SyntaxTokenType.Keyword },
            { "throw", SyntaxTokenType.Keyword },
            { "try", SyntaxTokenType.Keyword },
            { "typeof", SyntaxTokenType.Keyword },
            { "var", SyntaxTokenType.Keyword },
            { "void", SyntaxTokenType.Keyword },
            { "while", SyntaxTokenType.Keyword },
            { "with", SyntaxTokenType.Keyword },
            { "yield", SyntaxTokenType.Keyword },
            { "async", SyntaxTokenType.Keyword },
            { "await", SyntaxTokenType.Keyword },
            { "let", SyntaxTokenType.Keyword },
            { "static", SyntaxTokenType.Keyword },
            { "true", SyntaxTokenType.Keyword },
            { "false", SyntaxTokenType.Keyword },
            { "null", SyntaxTokenType.Keyword },
            { "undefined", SyntaxTokenType.Keyword }
        };

        TokenRegex = new Regex(
            @"\b(?<keyword>" + string.Join("|", Keywords.Keys) + @")\b|" +
            @"(?<comment>//.*?$|/\*.*?\*/)|" +
            @"(?<string>"".*?""|'.*?'|`[\s\S]*?`)|" +
            @"(?<number>\b\d+(\.\d+)?\b)|" +
            @"(?<identifier>\b[a-zA-Z_$][a-zA-Z0-9_$]*\b)",
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