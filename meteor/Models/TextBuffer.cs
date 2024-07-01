using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;

namespace meteor.Models;

public class TextBuffer : ReactiveObject
{
    private Rope _rope;
    private readonly Dictionary<long, long> _lineLengths;
    private int _cachedLineCount = -1;
    
    public TextBuffer()
    {
        _rope = new Rope(string.Empty);
        LineStarts = new List<long>();
        _lineLengths = new Dictionary<long, long>();
        UpdateLineCache();
    }

    public Rope Rope
    {
        get => _rope;
        set
        {
            this.RaiseAndSetIfChanged(ref _rope, value);
            UpdateLineCache();
        }
    }

    public void UpdateLongestLine(Dictionary<long, long> lineLengths, long longestLineLength)
    {
        LongestLineLength = lineLengths.Values.Max();
    }

    public long LineCount => _rope.LineCount;
    public long LongestLineLength { get; private set; }
    public long Length => _rope.Length;
    public List<long> LineStarts { get; }

    public event EventHandler LinesUpdated;

    public string GetLineText(long lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _rope.LineCount)
            return string.Empty;

        return _rope.GetLineText((int)lineIndex);
    }

    public void InsertText(long position, string text)
    {
        if (string.IsNullOrEmpty(text) || position < 0 || position > _rope.Length) return;

        _rope.Insert((int)position, text);

        // Update line cache and line lengths
        var startLine = (int)GetLineIndexFromPosition(position);
        UpdateLineCache(startLine, text.Count(c => c == '\n'), text); // Pass the inserted text
    
        LinesUpdated?.Invoke(this, EventArgs.Empty);
    }
    
    public void DeleteText(long start, long length)
    {
        if (length > 0)
        {
            _rope.Delete((int)start, (int)length);

            // Update line cache and line lengths
            var startLine = (int)GetLineIndexFromPosition(start);
            UpdateLineCache(startLine);
        
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
        if (lineIndex < 0 || lineIndex >= LineStarts.Count)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Invalid line index");

        return LineStarts[lineIndex];
    }

    public long GetLineEndPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineStarts.Count)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Invalid line index");

        // If it's the last line, the end position is the length of the rope
        if (lineIndex == LineStarts.Count - 1)
            return _rope.Length;

        // Otherwise, it's the start of the next line minus 1
        return LineStarts[lineIndex + 1] - 1;
    }

    public void UpdateLineCache(int startLine = 0, int linesInserted = 0, string insertedText = "")
    {
        if (startLine == 0)
        {
            // Initial update or complete re-calculation of line cache
        LineStarts.Clear();
        _lineLengths.Clear();
        LineStarts.Add(0);

        long lineStart = 0;
        LongestLineLength = 0; // Initialize to 0

        while (lineStart < _rope.Length)
        {
            var nextNewline = _rope.IndexOf('\n', (int)lineStart);
            if (nextNewline == -1)
            {
                _lineLengths[LineStarts.Count - 1] = _rope.Length - lineStart;
                break;
            }

            LineStarts.Add(nextNewline + 1);
            _lineLengths[LineStarts.Count - 2] = nextNewline - lineStart;
            lineStart = nextNewline + 1;

            // Update LongestLineLength within the loop
            LongestLineLength = Math.Max(LongestLineLength, nextNewline - lineStart);
        }
        }
        else
        {
            // Incremental update after an insertion
            var endLine = startLine + linesInserted; // Last line that might have been affected

            // Recalculate line starts and lengths from the modified line onwards
            var lineStart = LineStarts[startLine];
            for (var i = startLine; i <= endLine && lineStart < _rope.Length; i++)
            {
                var nextNewline = _rope.IndexOf('\n', (int)lineStart);
                if (nextNewline == -1 || nextNewline > _rope.Length) // Check for end of rope
                {
                    // Last line or no newline found
                    _lineLengths[i] = _rope.Length - lineStart;
                    break;
                }

                _lineLengths[i] = nextNewline - lineStart;
                lineStart = nextNewline + 1;

                // Update line starts for subsequent lines (if they exist)
                if (i + 1 < LineStarts.Count)
                    LineStarts[i + 1] = lineStart;
                else
                    LineStarts.Add(lineStart); // Add a new line start if needed
        }

            // If new lines were added, update the line starts after the insertion point
            for (var i = endLine + 1; i < LineStarts.Count; i++) LineStarts[i] += insertedText.Length;

            // Update LongestLineLength
            LongestLineLength = _lineLengths.Values.Max();
        }
    }

    public long GetLineLength(long lineIndex)
    {
        return _lineLengths.GetValueOrDefault(lineIndex, 0);
    }

    public long GetLineIndexFromPosition(long position)
    {
        var index = LineStarts.BinarySearch(position);
        if (index < 0)
        {
            index = ~index;
            index = Math.Max(0, index - 1);
        }

        return index;
    }
}