using meteor.Core.Interfaces;
using meteor.Core.Models.Events;

namespace meteor.Core.Models;

public class TextBuffer : ITextBuffer
{
    private List<string?> _lines = new() { "" };
    private readonly List<int> _cachedLineStarts = new() { 0 };
    private bool _isLineStartsCacheValid = true;

    public List<int> GetLineStarts()
    {
        if (!_isLineStartsCacheValid) UpdateLineStartsCache();
        return _cachedLineStarts;
    }

    public string Text => string.Join(Environment.NewLine, _lines);
    public int Length => Text.Length;
    public int LineCount => _lines.Count;

    public event EventHandler<TextChangedEventArgs> TextChanged;

    public string GetText(int start, int length)
    {
        if (start < 0 || start >= Length || length <= 0)
            return string.Empty;

        return Text.Substring(start, Math.Min(length, Length - start));
    }

    public void InsertText(int position, string text)
    {
        if (string.IsNullOrEmpty(text) || position < 0 || position > Length)
            return;

        var lineIndex = GetLineIndexFromPosition(position);
        var linePosition = position - GetLineStartPosition(lineIndex);

        var newLines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (newLines.Length == 1)
        {
            _lines[lineIndex] = _lines[lineIndex].Insert(linePosition, text);
        }
        else
        {
            var currentLine = _lines[lineIndex];
            var beforeInsertion = currentLine.Substring(0, linePosition);
            var afterInsertion = currentLine.Substring(linePosition);

            _lines[lineIndex] = beforeInsertion + newLines[0];
            _lines.InsertRange(lineIndex + 1, newLines.Skip(1).Take(newLines.Length - 2));
            _lines.Insert(lineIndex + newLines.Length - 1, newLines[^1] + afterInsertion);
        }

        _isLineStartsCacheValid = false;
        OnTextChanged(position, text, 0);
    }

    public void DeleteText(int start, int length)
    {
        if (length <= 0 || start < 0 || start >= Length)
            return;

        var startLineIndex = GetLineIndexFromPosition(start);
        var endLineIndex = GetLineIndexFromPosition(start + length);

        if (startLineIndex == endLineIndex)
        {
            var lineStart = GetLineStartPosition(startLineIndex);
            _lines[startLineIndex] = _lines[startLineIndex].Remove(start - lineStart, length);
        }
        else
        {
            var startLinePosition = start - GetLineStartPosition(startLineIndex);
            var endLinePosition = start + length - GetLineStartPosition(endLineIndex);

            var startLine = _lines[startLineIndex].Substring(0, startLinePosition);
            var endLine = _lines[endLineIndex].Substring(endLinePosition);

            _lines[startLineIndex] = startLine + endLine;
            _lines.RemoveRange(startLineIndex + 1, endLineIndex - startLineIndex);
        }

        _isLineStartsCacheValid = false;
        OnTextChanged(start, string.Empty, length);
    }

    public void SetText(string newText)
    {
        _lines = newText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        if (_lines.Count == 0)
            _lines.Add(string.Empty);

        _isLineStartsCacheValid = false;
        OnTextChanged(0, newText, Length);
    }

    public void Clear()
    {
        var oldLength = Length;
        _lines = new List<string?> { "" };
        _isLineStartsCacheValid = false;
        OnTextChanged(0, string.Empty, oldLength);
    }

    public string? GetLineText(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            return string.Empty;

        return _lines[lineIndex];
    }

    public int GetLineStartPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        return _lines.Take(lineIndex).Sum(line => line.Length + Environment.NewLine.Length);
    }

    public int GetLineEndPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        return GetLineStartPosition(lineIndex) + GetLineLength(lineIndex);
    }

    public int GetLineLength(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        return _lines[lineIndex].Length;
    }

    public int GetLineIndexFromPosition(int position)
    {
        if (position < 0 || position > Length)
            throw new ArgumentOutOfRangeException(nameof(position));

        var currentPosition = 0;
        for (var i = 0; i < LineCount; i++)
        {
            var lineLength = GetLineLength(i) + Environment.NewLine.Length;
            if (currentPosition + lineLength > position)
                return i;
            currentPosition += lineLength;
        }

        return LineCount - 1;
    }

    private void UpdateLineStartsCache()
    {
        _cachedLineStarts.Clear();
        _cachedLineStarts.Add(0);

        var currentPosition = 0;
        for (var i = 0; i < _lines.Count - 1; i++)
        {
            currentPosition += _lines[i].Length + Environment.NewLine.Length;
            _cachedLineStarts.Add(currentPosition);
        }

        _isLineStartsCacheValid = true;
    }

    protected virtual void OnTextChanged(int position, string insertedText, int deletedLength)
    {
        TextChanged?.Invoke(this, new TextChangedEventArgs(position, insertedText, deletedLength));
    }
}