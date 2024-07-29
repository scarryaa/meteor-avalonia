using System.Text.RegularExpressions;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class CompletionProvider : ICompletionProvider
{
    private readonly ITextBufferService _textBufferService;
    private readonly HashSet<string> _keywords;

    public CompletionProvider(ITextBufferService textBufferService)
    {
        _textBufferService = textBufferService;
        _keywords = new HashSet<string>
        {
            "keyword1", "keyword2", "keyword3"
        };
    }

    public List<CompletionItem> GetCompletions(int cursorPosition)
    {
        var text = _textBufferService.GetContent();
        var wordStart = FindWordStart(text, cursorPosition);
        var prefix = text.Substring(wordStart, cursorPosition - wordStart);

        var completions = new List<CompletionItem>();

        // Add keyword completions
        completions.AddRange(_keywords
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(k => new CompletionItem { Text = k, Kind = CompletionItemKind.Keyword }));

        // Add word completions from the document
        var wordPattern = new Regex(@"\b\w+\b");
        var matches = wordPattern.Matches(text);
        var words = new HashSet<string>(matches.Select(m => m.Value));

        completions.AddRange(words
            .Where(w => w.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !_keywords.Contains(w))
            .Select(w => new CompletionItem { Text = w, Kind = CompletionItemKind.Text }));

        Console.WriteLine($"Found {completions.Count} completions for '{prefix}'");
        return completions.OrderBy(c => c.Text).ToList();
    }

    private int FindWordStart(string text, int position)
    {
        while (position > 0 && char.IsLetterOrDigit(text[position - 1])) position--;
        return position;
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