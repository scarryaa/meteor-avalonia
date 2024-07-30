using System.Text.RegularExpressions;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class CompletionProvider : ICompletionProvider
{
    private readonly ITextBufferService _textBufferService;
    private readonly HashSet<string> _keywords;
    private HashSet<string> _cachedWords;
    private string _cachedText;
    private readonly Dictionary<string, int> _wordFrequency;

    public CompletionProvider(ITextBufferService textBufferService)
    {
        _textBufferService = textBufferService ?? throw new ArgumentNullException(nameof(textBufferService));
        _keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile", "while"
        };
        _wordFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<CompletionItem>> GetCompletionsAsync(int cursorPosition,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var text = _textBufferService.GetContent();
            var wordStart = FindWordStart(text, cursorPosition);
            var prefix = text.Substring(wordStart, cursorPosition - wordStart);
            var context = GetContext(text, cursorPosition);

            var completions = new HashSet<CompletionItem>(new CompletionItemComparer());

            await Task.WhenAll(
                AddKeywordCompletionsAsync(completions, prefix, context, cancellationToken),
                AddWordCompletionsAsync(completions, text, prefix, cancellationToken),
                AddFuzzyMatchCompletionsAsync(completions, prefix, cancellationToken)
            );

            return OrderCompletions(completions, prefix);
        }
        catch (Exception ex)
        {
            return Array.Empty<CompletionItem>();
        }
    }

    private Task AddKeywordCompletionsAsync(HashSet<CompletionItem> completions, string prefix, string context,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var relevantKeywords = GetContextRelevantKeywords(context);
            foreach (var keyword in relevantKeywords.Where(
                         k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                completions.Add(new CompletionItem { Text = keyword, Kind = CompletionItemKind.Keyword });
        }, cancellationToken);
    }

    private async Task AddWordCompletionsAsync(HashSet<CompletionItem> completions, string text, string prefix,
        CancellationToken cancellationToken)
    {
        await UpdateCachedWordsAsync(text, cancellationToken);
        await Task.Run(() =>
        {
            foreach (var word in _cachedWords.Where(w =>
                         w.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !_keywords.Contains(w)))
                completions.Add(new CompletionItem { Text = word, Kind = CompletionItemKind.Text });
        }, cancellationToken);
    }

    private Task AddFuzzyMatchCompletionsAsync(HashSet<CompletionItem> completions, string prefix,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            foreach (var match in GetFuzzyMatches(prefix, _cachedWords.Except(_keywords)))
                completions.Add(new CompletionItem { Text = match, Kind = CompletionItemKind.Text });
        }, cancellationToken);
    }

    private async Task UpdateCachedWordsAsync(string text, CancellationToken cancellationToken)
    {
        if (_cachedText != text)
            await Task.Run(() =>
            {
                var wordPattern = new Regex(@"\b[\w_]+\b");
                var matches = wordPattern.Matches(text);
                var newWords = new HashSet<string>(matches.Select(m => m.Value), StringComparer.OrdinalIgnoreCase);

                var partialWord = GetPartialWord(text);
                newWords.Remove(partialWord);

                _cachedWords = newWords;
                _cachedText = text;

                foreach (var word in _cachedWords)
                {
                    _wordFrequency.TryAdd(word, 0);
                    _wordFrequency[word]++;
                }
            }, cancellationToken);
    }

    private string GetPartialWord(string text)
    {
        var match = Regex.Match(text, @"\w+$");
        return match.Success ? match.Value : string.Empty;
    }

    private int FindWordStart(string text, int position)
    {
        while (position > 0 && (char.IsLetterOrDigit(text[position - 1]) || text[position - 1] == '_')) position--;
        return position;
    }

    private IEnumerable<string> GetFuzzyMatches(string prefix, IEnumerable<string> candidates)
    {
        return candidates.Where(c => FuzzyMatch(prefix, c))
            .OrderBy(c => LevenshteinDistance(prefix, c));
    }

    private bool FuzzyMatch(string pattern, string input)
    {
        int patternIdx = 0, inputIdx = 0;
        while (patternIdx < pattern.Length && inputIdx < input.Length)
        {
            if (char.ToLowerInvariant(pattern[patternIdx]) == char.ToLowerInvariant(input[inputIdx]))
                patternIdx++;
            inputIdx++;
        }

        return patternIdx == pattern.Length;
    }

    private int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; d[i, 0] = i++)
        {
        }

        for (var j = 0; j <= m; d[0, j] = j++)
        {
        }

        for (var i = 1; i <= n; i++)
        for (var j = 1; j <= m; j++)
        {
            var cost = t[j - 1] == s[i - 1] ? 0 : 1;
            d[i, j] = Math.Min(
                Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                d[i - 1, j - 1] + cost);
        }

        return d[n, m];
    }

    private IEnumerable<CompletionItem> OrderCompletions(IEnumerable<CompletionItem> completions, string prefix)
    {
        return completions
            .Where(c => !c.Text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(c => _wordFrequency.GetValueOrDefault(c.Text, 0))
            .ThenBy(c => c.Text.Length)
            .ThenBy(c => c.Text);
    }

    private string GetContext(string text, int cursorPosition)
    {
        var contextStart = Math.Max(0, cursorPosition - 100);
        return text.Substring(contextStart, cursorPosition - contextStart);
    }

    private IEnumerable<string> GetContextRelevantKeywords(string context)
    {
        var keywords = new HashSet<string>(_keywords, StringComparer.OrdinalIgnoreCase);

        if (context.Contains("class") || context.Contains("interface"))
            keywords.UnionWith(new[]
            {
                "public", "private", "protected", "internal", "virtual", "override", "abstract", "sealed"
            });

        if (context.Contains("if") || context.Contains("while") || context.Contains("for"))
            keywords.UnionWith(new[]
            {
                "break", "continue", "return", "throw"
            });

        return keywords;
    }
}

public class CompletionItemComparer : IEqualityComparer<CompletionItem>
{
    public bool Equals(CompletionItem x, CompletionItem y)
    {
        return x.Text == y.Text && x.Kind == y.Kind;
    }

    public int GetHashCode(CompletionItem obj)
    {
        return obj.Text.GetHashCode() ^ obj.Kind.GetHashCode();
    }
}

public class CompletionItem
{
    public string Text { get; set; }
    public CompletionItemKind Kind { get; set; }
}

public enum CompletionItemKind
{
    Text,
    Keyword,
    Snippet,
    Method,
    Function,
    Constructor,
    Field,
    Variable,
    Class,
    Interface,
    Module, 
    Property,
    Unit,
    Value,
    Enum,
    Color,
    File,
    Reference,
    Folder,
    EnumMember,
    Constant,
    Struct,
    Event,
    Operator,
    TypeParameter
}
