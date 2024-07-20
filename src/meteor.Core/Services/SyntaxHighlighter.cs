using meteor.Core.Enums.SyntaxHighlighting;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.SyntaxHighlighting;
using meteor.Core.Models.Text;

namespace meteor.Core.Services;

public class SyntaxHighlighter : ISyntaxHighlighter
{
    private const int ChunkSize = 4096;

    private List<SyntaxHighlightingResult> _cachedResults = new();
    private readonly Dictionary<int, List<SyntaxHighlightingResult>> _chunkCache = new();
    private readonly KeywordTrie _keywordTrie;
    private readonly ITabService _tabService;
    private string _lastProcessedText = string.Empty;

    public SyntaxHighlighter(ITabService tabService)
    {
        _tabService = tabService;
        _keywordTrie = new KeywordTrie(new[] { "if", "else", "for", "while", "return", "var" });
    }

    public IEnumerable<SyntaxHighlightingResult> Highlight(string text, TextChangeInfo changeInfo = null)
    {
        if (string.IsNullOrEmpty(text)) return Enumerable.Empty<SyntaxHighlightingResult>();

        var textBufferService = _tabService.GetActiveTextBufferService();

        if (changeInfo != null)
        {
            UpdateHighlighting(changeInfo, textBufferService);
        }
        else if (textBufferService.Length != _lastProcessedText.Length || text != _lastProcessedText)
        {
            _cachedResults.Clear();
            _chunkCache.Clear();
            HighlightEntireText(text);
        }

        _lastProcessedText = text;
        return _cachedResults;
    }

    private void HighlightEntireText(string text)
    {
        var span = text.AsSpan();
        var inString = false;
        var stringStart = -1;

        for (var i = 0; i < span.Length; i += ChunkSize)
        {
            var length = Math.Min(ChunkSize, span.Length - i);
            ProcessChunk(span.Slice(i, length), i, ref inString, ref stringStart);
        }

        // Handle case where string extends beyond the last chunk
        if (inString && stringStart != -1)
            _cachedResults.Add(new SyntaxHighlightingResult
            {
                StartIndex = stringStart,
                Length = span.Length - stringStart,
                Type = SyntaxHighlightingType.String
            });
    }

    private void UpdateHighlighting(TextChangeInfo changeInfo, ITextBufferService textBufferService)
    {
        var startChunk = changeInfo.StartPosition / ChunkSize;
        var endChunk = (changeInfo.EndPosition + changeInfo.NewText.Length) / ChunkSize;

        // Clear affected chunks from cache
        for (var i = startChunk; i <= endChunk; i++) _chunkCache.Remove(i);

        // Remove highlights in the changed region
        _cachedResults.RemoveAll(r =>
            r.StartIndex >= changeInfo.StartPosition && r.StartIndex < changeInfo.EndPosition);

        // Adjust indices of highlights after the change
        var adjustment = changeInfo.NewText.Length - (changeInfo.EndPosition - changeInfo.StartPosition);
        for (var i = 0; i < _cachedResults.Count; i++)
            if (_cachedResults[i].StartIndex >= changeInfo.EndPosition)
                _cachedResults[i] = new SyntaxHighlightingResult
                {
                    StartIndex = _cachedResults[i].StartIndex + adjustment,
                    Length = _cachedResults[i].Length,
                    Type = _cachedResults[i].Type
                };

        // Highlight the changed region and surrounding context
        var contextSize = ChunkSize; // Use one chunk size as context
        var highlightStart = Math.Max(0, changeInfo.StartPosition - contextSize);
        var highlightEnd = Math.Min(textBufferService.Length,
            changeInfo.StartPosition + changeInfo.NewText.Length + contextSize);

        var changedText = textBufferService.Substring(highlightStart, highlightEnd - highlightStart);
        var span = changedText.AsSpan();

        var inString = false;
        var stringStart = -1;

        ProcessChunk(span, highlightStart, ref inString, ref stringStart);

        // Merge overlapping or adjacent results
        MergeOverlappingResults();
    }

    private void MergeOverlappingResults()
    {
        _cachedResults = _cachedResults.OrderBy(r => r.StartIndex).ToList();
        var i = 0;
        while (i < _cachedResults.Count - 1)
        {
            var current = _cachedResults[i];
            var next = _cachedResults[i + 1];

            if (current.Type == next.Type && 
                (current.StartIndex + current.Length >= next.StartIndex || 
                 current.StartIndex + current.Length == next.StartIndex - 1))
            {
                _cachedResults[i] = new SyntaxHighlightingResult
                {
                    StartIndex = current.StartIndex,
                    Length = Math.Max(current.StartIndex + current.Length, next.StartIndex + next.Length) - current.StartIndex,
                    Type = current.Type
                };
                _cachedResults.RemoveAt(i + 1);
            }
            else
            {
                i++;
            }
        }
    }

    private void ProcessChunk(ReadOnlySpan<char> span, int offset, ref bool inString, ref int stringStart)
    {
        var chunkKey = offset / ChunkSize;
        if (_chunkCache.TryGetValue(chunkKey, out var cachedResults))
        {
            _cachedResults.AddRange(cachedResults);
            return;
        }

        var chunkResults = new List<SyntaxHighlightingResult>();
        HighlightKeywords(span, offset, chunkResults);
        HighlightComments(span, offset, chunkResults);
        HighlightStrings(span, offset, chunkResults, ref inString, ref stringStart);

        _chunkCache[chunkKey] = chunkResults;
        _cachedResults.AddRange(chunkResults);
    }

    private void HighlightKeywords(ReadOnlySpan<char> span, int offset, List<SyntaxHighlightingResult> results)
    {
        var startIndex = 0;
        while (startIndex < span.Length)
        {
            // Find the next letter
            while (startIndex < span.Length && !char.IsLetter(span[startIndex])) startIndex++;

            if (startIndex >= span.Length) break;

            // Find the end of the word
            var endIndex = startIndex + 1;
            while (endIndex < span.Length && char.IsLetterOrDigit(span[endIndex])) endIndex++;

            // Check if it's a keyword
            var word = span.Slice(startIndex, endIndex - startIndex);
            if (_keywordTrie.Contains(word))
            {
                results.Add(new SyntaxHighlightingResult
                {
                    StartIndex = offset + startIndex,
                    Length = endIndex - startIndex,
                    Type = SyntaxHighlightingType.Keyword
                });
            }

            startIndex = endIndex;
        }
    }

    private void HighlightComments(ReadOnlySpan<char> span, int offset, List<SyntaxHighlightingResult> results)
    {
        var index = 0;
        while (index < span.Length - 1)
            if (IsStartOfSingleLineComment(span, index))
                index = HighlightSingleLineComment(span, offset, index, results);
            else if (IsStartOfMultiLineComment(span, index))
                index = HighlightMultiLineComment(span, offset, index, results);
            else
                index++;
    }

    private bool IsStartOfSingleLineComment(ReadOnlySpan<char> span, int index)
    {
        return span[index] == '/' && span[index + 1] == '/';
    }

    private bool IsStartOfMultiLineComment(ReadOnlySpan<char> span, int index)
    {
        return span[index] == '/' && span[index + 1] == '*';
    }

    private int HighlightSingleLineComment(ReadOnlySpan<char> span, int offset, int start,
        List<SyntaxHighlightingResult> results)
    {
        var end = span.Slice(start).IndexOf('\n');
        if (end == -1) end = span.Length;
        else end += start;

        results.Add(new SyntaxHighlightingResult
        {
            StartIndex = offset + start,
            Length = end - start,
            Type = SyntaxHighlightingType.Comment
        });

        return end;
    }

    private int HighlightMultiLineComment(ReadOnlySpan<char> span, int offset, int start,
        List<SyntaxHighlightingResult> results)
    {
        var index = start + 2;
        while (index < span.Length - 1 && !(span[index] == '*' && span[index + 1] == '/')) index++;
        if (index < span.Length - 1) index += 2;

        results.Add(new SyntaxHighlightingResult
        {
            StartIndex = offset + start,
            Length = index - start,
            Type = SyntaxHighlightingType.Comment
        });

        return index;
    }

    private void HighlightStrings(ReadOnlySpan<char> span, int offset, List<SyntaxHighlightingResult> results,
        ref bool inString, ref int stringStart)
    {
        for (var i = 0; i < span.Length; i++)
            if (span[i] == '"' && (i == 0 || span[i - 1] != '\\'))
            {
                if (!inString)
                {
                    stringStart = offset + i;
                    inString = true;
                }
                else
                {
                    results.Add(new SyntaxHighlightingResult
                    {
                        StartIndex = stringStart,
                        Length = offset + i - stringStart + 1,
                        Type = SyntaxHighlightingType.String
                    });
                    inString = false;
                    stringStart = -1;
                }
            }
    }
}