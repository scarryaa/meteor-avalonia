namespace meteor.Core.Models.SyntaxHighlighting;

public class KeywordTrie
{
    private sealed class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = new();
        public bool IsEndOfWord { get; set; }
    }

    private readonly TrieNode _root = new();

    public KeywordTrie(IEnumerable<string> keywords)
    {
        foreach (var keyword in keywords) Insert(keyword);
    }

    private void Insert(string keyword)
    {
        var node = _root;
        foreach (var ch in keyword)
        {
            if (!node.Children.TryGetValue(ch, out var child))
            {
                child = new TrieNode();
                node.Children[ch] = child;
            }

            node = child;
        }

        node.IsEndOfWord = true;
    }

    public bool Contains(ReadOnlySpan<char> word)
    {
        var node = _root;
        foreach (var ch in word)
        {
            if (!node.Children.TryGetValue(ch, out var child)) return false;
            node = child;
        }

        return node.IsEndOfWord;
    }
}