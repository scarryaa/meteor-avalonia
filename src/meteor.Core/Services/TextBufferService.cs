using System.Text;
using meteor.Core.Entities;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class TextBufferService : ITextBufferService
{
    private readonly StringBuilder _stringBuilder = new();
    private readonly TextBuffer _textBuffer;
    private char[] _spanBuffer = new char[1024];
    private readonly SortedDictionary<int, int> _lineIndices = new();
    private bool _lineIndicesNeedUpdate = true;

    public TextBufferService(string initialText = "")
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
        _lineIndices[0] = 1; // First line always starts at index 0

        var lineNumber = 1;
        _textBuffer.IndexedIterate((c, i) =>
        {
            if (c == '\n')
            {
                lineNumber++;
                _lineIndices[i + 1] = lineNumber;
            }
        });

        _lineIndicesNeedUpdate = false;
    }

    public int IndexOf(char value, int startIndex = 0)
    {
        for (var i = startIndex; i < Length; i++)
            if (_textBuffer[i] == value)
                return i;
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

    public void ReplaceAll(string newText)
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
        if (length > _spanBuffer.Length) _spanBuffer = new char[Math.Max(length, _spanBuffer.Length * 2)];
        _textBuffer.GetTextSegment(start, length, _spanBuffer);
        return new ReadOnlySpan<char>(_spanBuffer, 0, length);
    }

    public int GetLineNumberFromPosition(int index)
    {
        if (index < 0 || index > Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        UpdateLineIndices();

        var entry = _lineIndices.LastOrDefault(kvp => kvp.Key <= index);
        return entry.Value;
    }

    public void AppendTo(StringBuilder sb)
    {
        _textBuffer.GetTextSegment(0, _textBuffer.Length, sb);
    }

    public int GetLineCount()
    {
        UpdateLineIndices();
        return _lineIndices.Values.Last();
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
            throw new ArgumentOutOfRangeException("start");

        _stringBuilder.Clear();
        _textBuffer.GetTextSegment(start, length, _stringBuilder);
        return _stringBuilder.ToString();
    }
}