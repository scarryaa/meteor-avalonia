using System;
using System.Collections.Generic;
using ReactiveUI;

public class TextBuffer : ReactiveObject
{
    private Rope _rope;
    private readonly Dictionary<long, long> _lineLengths;
    private long _longestLineLength;
    private double _lineHeight;

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

        if (lineIndex == LineStarts.Count - 1)
            return _rope.Length;

        return LineStarts[lineIndex + 1] - 1;
    }

    public long GetLineLength(long lineIndex)
    {
        return _lineLengths.GetValueOrDefault(lineIndex, 0);
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

            _lineLengths[LineStarts.Count - 1] = nextNewline - lineStart;
            LongestLineLength = Math.Max(LongestLineLength, _lineLengths[LineStarts.Count - 1]);

            LineStarts.Add(nextNewline + 1);
            lineStart = nextNewline + 1;
        }

        UpdateTotalHeight();
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

    private void UpdateTotalHeight()
    {
        TotalHeight = LineCount * LineHeight + 6;
        this.RaisePropertyChanged(nameof(TotalHeight));
    }
}
