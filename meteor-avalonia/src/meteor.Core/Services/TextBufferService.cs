using System.Buffers;
using System.Text;
using meteor.Core.Entities;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class TextBufferService : ITextBufferService
{
    private readonly StringBuilder _stringBuilder = new();
    private readonly TextBuffer _textBuffer;
    private char[] _spanBuffer = Array.Empty<char>();
    private readonly SortedList<int, int> _lineIndices = new();
    private bool _lineIndicesNeedUpdate = true;
    private readonly ArrayPool<char> _arrayPool = ArrayPool<char>.Shared;

    public TextBufferService(string? initialText = "")
    {
        _textBuffer = new TextBuffer(initialText);
        UpdateLineIndices();
    }

    public int Length => _textBuffer.Length;
    public char this[int index] => _textBuffer[index];

    public void Insert(int index, string text)
    {
        _textBuffer.Insert(index, text);
        _lineIndicesNeedUpdate = true;
    }

    public void Delete(int index, int length)
    {
        _textBuffer.Delete(index, length);
        _lineIndicesNeedUpdate = true;
    }

    public (int X, int Y) CalculatePositionFromIndex(int index, ITextMeasurer textMeasurer)
    {
        _textBuffer.EnsureAllInsertionsProcessed();
        var bufferLength = _textBuffer.Length;

        if (index < 0 || index > bufferLength)
            throw new ArgumentOutOfRangeException(nameof(index));

        UpdateLineIndices();

        var lineNumber = GetLineNumberFromPosition(index);
        var lineStartIndex = _lineIndices.Keys.TakeWhile(i => i <= index).Last();
        var x = (index - lineStartIndex) * textMeasurer.GetCharWidth();
        var y = (lineNumber - 1) * textMeasurer.GetLineHeight();

        return (Convert.ToInt32(x), Convert.ToInt32(y));
    }

    private void UpdateLineIndices()
    {
        if (!_lineIndicesNeedUpdate) return;

        _lineIndices.Clear();
        _lineIndices.Add(0, 1); // First line always starts at index 0

        var lineNumber = 1;
        var length = _textBuffer.Length;
        for (var i = 0; i < length; i++)
            if (_textBuffer[i] == '\n')
            {
                lineNumber++;
                _lineIndices.Add(i + 1, lineNumber);
            }

        _lineIndicesNeedUpdate = false;
    }

    public int IndexOf(char value, int startIndex = 0)
    {
        var length = Length;
        var chunk = 4096;
        var buffer = new char[chunk];

        for (var i = startIndex; i < length; i += chunk)
        {
            var readLength = Math.Min(chunk, length - i);
            _textBuffer.GetTextSegment(i, readLength, buffer);
            for (var j = 0; j < readLength; j++)
                if (buffer[j] == value)
                    return i + j;
        }

        return -1;
    }


    public int LastIndexOf(char value, int startIndex = -1)
    {
        var start = startIndex == -1 ? Length - 1 : startIndex;
        for (var i = start; i >= 0; i--)
            if (_textBuffer[i] == value)
                return i;
        return -1;
    }

    public void GetTextSegment(int start, int length, StringBuilder output)
    {
        _textBuffer.GetTextSegment(start, length, output);
    }

    public void GetTextSegment(int start, int length, char[] output)
    {
        _textBuffer.GetTextSegment(start, length, output);
    }

    public void ReplaceAll(string? newText)
    {
        _textBuffer.ReplaceAll(newText);
        _lineIndicesNeedUpdate = true;
    }

    public void Iterate(Action<char> action)
    {
        _textBuffer.Iterate(action);
    }

    public ReadOnlySpan<char> AsSpan(int start, int length)
    {
        if (start < 0 || length < 0 || start + length > _textBuffer.Length)
            throw new ArgumentOutOfRangeException(nameof(start));

        if (length == 0) return ReadOnlySpan<char>.Empty;

        if (start >= _textBuffer.Length) return ReadOnlySpan<char>.Empty;

        var rentedArray = _arrayPool.Rent(length);
        try
        {
            _textBuffer.GetTextSegment(start, length, rentedArray);
            return new ReadOnlySpan<char>(rentedArray, 0, length);
        }
        finally
        {
            _arrayPool.Return(rentedArray);
        }
    }

    public int GetLineNumberFromPosition(int index)
    {
        if (index < 0 || index > Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        UpdateLineIndices();

        var low = 0;
        var high = _lineIndices.Count - 1;

        while (low <= high)
        {
            var mid = (low + high) / 2;
            if (_lineIndices.Keys[mid] == index)
                return _lineIndices.Values[mid];
            if (_lineIndices.Keys[mid] < index)
                low = mid + 1;
            else
                high = mid - 1;
        }

        return _lineIndices.Values[high];
    }

    public void AppendTo(StringBuilder sb)
    {
        _textBuffer.GetTextSegment(0, _textBuffer.Length, sb);
    }

    public int GetLineCount()
    {
        UpdateLineIndices();
        return _lineIndices.Values[^1];
    }

    public string GetLineText(int lineNumber)
    {
        UpdateLineIndices();

        if (lineNumber < 1 || lineNumber > GetLineCount())
            throw new ArgumentOutOfRangeException(nameof(lineNumber));

        var startIndex = _lineIndices.FirstOrDefault(kvp => kvp.Value == lineNumber).Key;
        var endIndex = _lineIndices.FirstOrDefault(kvp => kvp.Value == lineNumber + 1).Key;

        if (endIndex == 0) // Last line
            endIndex = _textBuffer.Length;
        else
            endIndex--; // Exclude the newline character

        return Substring(startIndex, endIndex - startIndex);
    }

    public string Substring(int start, int length)
    {
        if (start < 0 || length < 0 || start + length > _textBuffer.Length)
            throw new ArgumentOutOfRangeException(nameof(start));

        _stringBuilder.Clear();
        _textBuffer.GetTextSegment(start, length, _stringBuilder);
        return _stringBuilder.ToString();
    }
}