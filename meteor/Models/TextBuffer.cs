using System;
using System.Collections.Generic;
using System.Linq;
using meteor.Interfaces;
using ReactiveUI;

public class TextBuffer : ReactiveObject, ITextBuffer
{
    private IRope _rope;
    private readonly Dictionary<long, long> _lineLengths;
    private long _longestLineLength;
    private double _lineHeight;

    // Cache for line start positions and line indices
    private readonly Dictionary<int, long> _lineStartCache = new();
    private readonly Dictionary<long, int> _lineIndexCache = new();

    public TextBuffer()
    {
        _rope = new Rope(string.Empty);
        LineStarts = new List<long>();
        _lineLengths = new Dictionary<long, long>();
        UpdateLineCache();
    }

    public string Text => _rope.GetText();

    public IRope Rope
    {
        get => _rope;
        set
        {
            this.RaiseAndSetIfChanged(ref _rope, value);
        }
    }

    public long LineCount => _rope.LineCount;

    public double LineHeight
    {
        get => _lineHeight;
        set
        {
            this.RaiseAndSetIfChanged(ref _lineHeight, value);
            UpdateTotalHeight();
        }
    }

    public double TotalHeight { get; private set; }

    public long LongestLineLength
    {
        get => _longestLineLength;
        private set => this.RaiseAndSetIfChanged(ref _longestLineLength, value);
    }

    public long Length => _rope.Length;

    public List<long> LineStarts { get; }

    public event EventHandler LinesUpdated;
    public event EventHandler TextChanged;

    public string GetLineText(long lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _rope.LineCount)
            return string.Empty;

        var lineStart = GetLineStartPosition((int)lineIndex);
        var lineEnd = GetLineEndPosition((int)lineIndex);
        return _rope.GetText((int)lineStart, (int)(lineEnd - lineStart + 1));
    }

    public void SetText(string newText)
    {
        // Clear the current text buffer
        Clear();

        // Insert the new text
        InsertText(0, newText);
        UpdateLineCache();
    }

    public void Clear()
    {
        _rope = new Rope(string.Empty);
        LineStarts.Clear();
        LineStarts.Add(0);
        _lineLengths.Clear();
        _lineLengths[0] = 0;
        LongestLineLength = 0;
        UpdateTotalHeight();
        LinesUpdated?.Invoke(this, EventArgs.Empty);
    }

    public string GetText(long start, long end)
    {
        return Text.Substring((int)start, (int)end);
    }

    public void InsertText(long position, string text)
    {
        if (string.IsNullOrEmpty(text) || position < 0 || position > _rope.Length) return;

        _rope.Insert((int)position, text);
        UpdateLineCacheAfterInsertion(position, text);
        TextChanged?.Invoke(this, EventArgs.Empty);
        LinesUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void DeleteText(long start, long length)
    {
        if (length > 0)
        {
            _rope.Delete((int)start, (int)length);
            UpdateLineCacheAfterDeletion(start, length);
            LinesUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsLineSelected(int lineIndex, long selectionStart, long selectionEnd)
    {
        if (lineIndex < 0 || lineIndex >= LineStarts.Count)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Invalid line index");

        var lineStart = LineStarts[lineIndex];
        var lineEnd = GetLineEndPosition(lineIndex);

        return selectionStart <= lineEnd && selectionEnd >= lineStart;
    }

    public long GetLineStartPosition(int lineIndex)
    {
        if (_lineStartCache.TryGetValue(lineIndex, out var cachedPosition)) return cachedPosition;

        if (lineIndex < 0 || lineIndex >= LineStarts.Count)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Invalid line index");

        var position = LineStarts[lineIndex];
        _lineStartCache[lineIndex] = position;
        return position;
    }

    public long GetLineEndPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineStarts.Count)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Invalid line index");

        if (lineIndex == LineStarts.Count - 1)
            return _rope.Length - 1;

        return LineStarts[lineIndex + 1] - 1;
    }

    public long GetLineLength(long lineIndex)
    {
        return _lineLengths.GetValueOrDefault(lineIndex, 0);
    }

    private void UpdateLineCacheAfterInsertion(long position, string text)
    {
        ClearCache();

        var insertionLine = GetLineIndexFromPosition(position);
        var newLineCount = text.Count(c => c == '\n');

        // Ensure the insertion line exists in the dictionary
        if (!_lineLengths.ContainsKey(insertionLine)) _lineLengths[insertionLine] = 0;

        if (newLineCount == 0)
        {
            // Update the length of the affected line
            _lineLengths[insertionLine] += text.Length;
        }
        else
        {
            // Shift existing line starts after the insertion point
            for (var i = LineStarts.Count - 1; i > insertionLine; i--) LineStarts[i] += text.Length;

            // Insert new line starts
            var newLinePositions = text.Select((c, i) => c == '\n' ? i : -1)
                .Where(i => i != -1)
                .Select(i => position + i + 1)
                .ToList();

            LineStarts.InsertRange((int)insertionLine + 1, newLinePositions);

            // Update line lengths
            var lineStartPositions = new List<long> { position };
            lineStartPositions.AddRange(newLinePositions);
            lineStartPositions.Add(position + text.Length);

            for (var i = 0; i <= newLineCount; i++)
            {
                var lineLength = lineStartPositions[i + 1] - lineStartPositions[i];
                _lineLengths[insertionLine + i] = lineLength;
            }

            // Shift existing line lengths
            for (long i = LineStarts.Count - 1; i > insertionLine + newLineCount; i--)
                if (_lineLengths.ContainsKey(i - newLineCount)) _lineLengths[i] = _lineLengths[i - newLineCount];
        }

        LongestLineLength = _lineLengths.Values.Max();
        UpdateTotalHeight();
    }

    private void UpdateLineCacheAfterDeletion(long start, long length)
    {
        ClearCache();

        var startLine = GetLineIndexFromPosition(start);
        var endLine = GetLineIndexFromPosition(start + length);

        // Ensure startLine is always a valid key in the dictionary
        if (!_lineLengths.ContainsKey(startLine)) _lineLengths[startLine] = 0;

        if (startLine == endLine)
        {
            // Update the length of the affected line
            _lineLengths[startLine] = Math.Max(0, _lineLengths[startLine] - length);
        }
        else
        {
            // Remove line starts for deleted lines
            LineStarts.RemoveRange((int)startLine + 1, (int)(endLine - startLine));

            // Update remaining line starts
            for (var i = (int)startLine + 1; i < LineStarts.Count; i++) LineStarts[i] -= length;

            // Remove lengths for deleted lines
            for (var i = startLine + 1; i <= endLine; i++) _lineLengths.Remove(i);

            // Recalculate line length for the merged line
            _lineLengths[startLine] =
                (startLine + 1 < LineStarts.Count ? LineStarts[(int)startLine + 1] : _rope.Length) -
                LineStarts[(int)startLine];
        }

        // Ensure we always have at least one line
        if (LineStarts.Count == 0)
        {
            LineStarts.Add(0);
            _lineLengths[0] = 0;
        }

        // Recalculate longest line length
        LongestLineLength = _lineLengths.Values.Count > 0 ? _lineLengths.Values.Max() : 0;

        UpdateTotalHeight();
    }

    public void UpdateLineCache()
    {
        ClearCache();

        LineStarts.Clear();
        _lineLengths.Clear();
        LineStarts.Add(0);

        long lineStart = 0;
        LongestLineLength = 0;

        while (lineStart < _rope.Length)
        {
            var nextNewline = _rope.IndexOf('\n', (int)lineStart);
            if (nextNewline == -1)
            {
                // Handle the last line if there are no more newlines
                var lastLineLength = _rope.Length - lineStart;
                _lineLengths[LineStarts.Count - 1] = lastLineLength;
                LongestLineLength = Math.Max(LongestLineLength, lastLineLength);
                break;
            }

            var currentLineLength = nextNewline - lineStart + 1; // Include newline character in length
            _lineLengths[LineStarts.Count - 1] = currentLineLength;
            LongestLineLength = Math.Max(LongestLineLength, currentLineLength);

            LineStarts.Add(nextNewline + 1);
            lineStart = nextNewline + 1;
        }

        // Handle the case where there are no lines at all
        if (_rope.Length == 0) LongestLineLength = 0;

        UpdateTotalHeight();
    }

    public long GetLineIndexFromPosition(long position)
    {
        if (_lineIndexCache.TryGetValue(position, out var cachedIndex)) return cachedIndex;

        var index = LineStarts.BinarySearch(position);
        if (index < 0)
        {
            index = ~index;
            index = Math.Max(0, index - 1);
        }

        _lineIndexCache[position] = index;
        return index;
    }

    private void ClearCache()
    {
        _lineStartCache.Clear();
        _lineIndexCache.Clear();
    }

    private void UpdateTotalHeight()
    {
        TotalHeight = LineCount * LineHeight + 6;
        this.RaisePropertyChanged(nameof(TotalHeight));
    }
}
