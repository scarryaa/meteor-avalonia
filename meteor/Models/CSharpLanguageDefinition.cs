using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using meteor.Enums;
using meteor.Models;

public class CSharpLanguageDefinition : LanguageDefinition
{
    public CSharpLanguageDefinition()
    {
        Keywords = new Dictionary<string, SyntaxTokenType>
        {
            { "abstract", SyntaxTokenType.Keyword },
            { "as", SyntaxTokenType.Keyword },
            { "base", SyntaxTokenType.Keyword },
            { "bool", SyntaxTokenType.Keyword },
            { "break", SyntaxTokenType.Keyword },
            { "byte", SyntaxTokenType.Keyword },
            { "case", SyntaxTokenType.Keyword },
            { "catch", SyntaxTokenType.Keyword },
            { "char", SyntaxTokenType.Keyword },
            { "checked", SyntaxTokenType.Keyword },
            { "class", SyntaxTokenType.Keyword },
            { "const", SyntaxTokenType.Keyword },
            { "continue", SyntaxTokenType.Keyword },
            { "decimal", SyntaxTokenType.Keyword },
            { "default", SyntaxTokenType.Keyword },
            { "delegate", SyntaxTokenType.Keyword },
            { "do", SyntaxTokenType.Keyword },
            { "double", SyntaxTokenType.Keyword },
            { "else", SyntaxTokenType.Keyword },
            { "enum", SyntaxTokenType.Keyword },
            { "event", SyntaxTokenType.Keyword },
            { "explicit", SyntaxTokenType.Keyword },
            { "extern", SyntaxTokenType.Keyword },
            { "false", SyntaxTokenType.Keyword },
            { "finally", SyntaxTokenType.Keyword },
            { "fixed", SyntaxTokenType.Keyword },
            { "float", SyntaxTokenType.Keyword },
            { "for", SyntaxTokenType.Keyword },
            { "foreach", SyntaxTokenType.Keyword },
            { "goto", SyntaxTokenType.Keyword },
            { "if", SyntaxTokenType.Keyword },
            { "implicit", SyntaxTokenType.Keyword },
            { "in", SyntaxTokenType.Keyword },
            { "int", SyntaxTokenType.Keyword },
            { "interface", SyntaxTokenType.Keyword },
            { "internal", SyntaxTokenType.Keyword },
            { "is", SyntaxTokenType.Keyword },
            { "lock", SyntaxTokenType.Keyword },
            { "long", SyntaxTokenType.Keyword },
            { "namespace", SyntaxTokenType.Keyword },
            { "new", SyntaxTokenType.Keyword },
            { "null", SyntaxTokenType.Keyword },
            { "object", SyntaxTokenType.Keyword },
            { "operator", SyntaxTokenType.Keyword },
            { "out", SyntaxTokenType.Keyword },
            { "override", SyntaxTokenType.Keyword },
            { "params", SyntaxTokenType.Keyword },
            { "private", SyntaxTokenType.Keyword },
            { "protected", SyntaxTokenType.Keyword },
            { "public", SyntaxTokenType.Keyword },
            { "readonly", SyntaxTokenType.Keyword },
            { "ref", SyntaxTokenType.Keyword },
            { "return", SyntaxTokenType.Keyword },
            { "sbyte", SyntaxTokenType.Keyword },
            { "sealed", SyntaxTokenType.Keyword },
            { "short", SyntaxTokenType.Keyword },
            { "sizeof", SyntaxTokenType.Keyword },
            { "stackalloc", SyntaxTokenType.Keyword },
            { "static", SyntaxTokenType.Keyword },
            { "string", SyntaxTokenType.Keyword },
            { "struct", SyntaxTokenType.Keyword },
            { "switch", SyntaxTokenType.Keyword },
            { "this", SyntaxTokenType.Keyword },
            { "throw", SyntaxTokenType.Keyword },
            { "true", SyntaxTokenType.Keyword },
            { "try", SyntaxTokenType.Keyword },
            { "typeof", SyntaxTokenType.Keyword },
            { "uint", SyntaxTokenType.Keyword },
            { "ulong", SyntaxTokenType.Keyword },
            { "unchecked", SyntaxTokenType.Keyword },
            { "unsafe", SyntaxTokenType.Keyword },
            { "ushort", SyntaxTokenType.Keyword },
            { "using", SyntaxTokenType.Keyword },
            { "virtual", SyntaxTokenType.Keyword },
            { "void", SyntaxTokenType.Keyword },
            { "volatile", SyntaxTokenType.Keyword },
            { "while", SyntaxTokenType.Keyword }
        };

        TokenRegex = new Regex(
            @"\b(?<keyword>" + string.Join("|", Keywords.Keys) + @")\b|" +
            @"(?<comment>//.*?$|/\*.*?\*/)|" +
            @"(?<string>@""(?:[^""]|"""")*""|""(?:\\.|[^\\""])*"")|" +
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

        Console.WriteLine($"Generated {tokens.Count} tokens for line {lineIndex}");
        return tokens;
    }
}