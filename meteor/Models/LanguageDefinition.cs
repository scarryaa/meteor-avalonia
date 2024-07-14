using System.Collections.Generic;
using System.Text.RegularExpressions;
using meteor.Enums;

namespace meteor.Models;

public abstract class LanguageDefinition
{
    protected Dictionary<string, SyntaxTokenType> Keywords;
    protected Regex TokenRegex;

    public abstract List<SyntaxToken> TokenizeQuickly(string line, int lineIndex);
}