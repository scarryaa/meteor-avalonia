using System;
using System.Collections.Generic;
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
        UpdateLineCache();
        LinesUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void DeleteText(long start, long length)
    {
        if (length > 0)
        {
            _rope.Delete((int)start, (int)length);
            UpdateLineCache();
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

    public void UpdateLineCache()
    {
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
                _lineLengths[LineStarts.Count - 1] = _rope.Length - lineStart;
                break;
            }

            LineStarts.Add(nextNewline + 1);
            _lineLengths[LineStarts.Count - 2] = nextNewline - lineStart;
            lineStart = nextNewline + 1;
        }

        LongestLineLength = _rope.LongestLineLength;
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