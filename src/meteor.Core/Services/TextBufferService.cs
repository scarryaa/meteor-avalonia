using System.Text;
using meteor.Core.Entities;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class TextBufferService : ITextBufferService
{
    private readonly StringBuilder _stringBuilder = new();
    private readonly TextBuffer _textBuffer;
    private char[] _spanBuffer = new char[1024];
    private readonly List<int> _lineStartIndices = new() { 0 };
    private bool _lineIndicesNeedUpdate = true;

    public TextBufferService(string initialText = "")
    {
        _textBuffer = new TextBuffer(initialText);
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

        var x = 0.0;
        var y = 0.0;

        for (var i = 0; i < index; i++)
            if (_textBuffer[i] == '\n')
            {
                y += textMeasurer.GetLineHeight();
                x = 0;
            }
            else
            {
                x += textMeasurer.GetCharWidth();
            }

        return (Convert.ToInt32(x), Convert.ToInt32(y));
    }

    private void UpdateLineIndices()
    {
        if (!_lineIndicesNeedUpdate) return;

        _lineStartIndices.Clear();
        _lineStartIndices.Add(0);

        for (var i = 0; i < _textBuffer.Length; i++)
            if (_textBuffer[i] == '\n')
                _lineStartIndices.Add(i + 1);

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

        var lineNumber = 1;

        for (var i = 0; i < index; i++)
            if (_textBuffer[i] == '\n')
                lineNumber++;

        return lineNumber;
    }

    public void AppendTo(StringBuilder sb)
    {
        _textBuffer.GetTextSegment(0, _textBuffer.Length, sb);
    }

    public int GetLineCount()
    {
        UpdateLineIndices();
        return _lineStartIndices.Count;
    }

    public string GetLineText(int lineNumber)
    {
        UpdateLineIndices();

        if (lineNumber < 1 || lineNumber > _lineStartIndices.Count)
            throw new ArgumentOutOfRangeException(nameof(lineNumber));

        var startIndex = _lineStartIndices[lineNumber - 1];
        var endIndex = lineNumber < _lineStartIndices.Count
            ? _lineStartIndices[lineNumber] - 1 // -1 to exclude the newline character
            : _textBuffer.Length;

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