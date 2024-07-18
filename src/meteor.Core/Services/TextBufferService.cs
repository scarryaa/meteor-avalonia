using System.Text;
using meteor.Core.Entities;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class TextBufferService : ITextBufferService
{
    private readonly TextBuffer _textBuffer;
    private readonly StringBuilder _stringBuilder = new();

    public TextBufferService(string initialText = "")
    {
        _textBuffer = new TextBuffer(initialText);
    }

    public int Length => _textBuffer.Length;
    public char this[int index] => _textBuffer[index];

    public void Insert(int index, string text)
    {
        _textBuffer.Insert(index, text);
    }

    public void Delete(int index, int length)
    {
        _textBuffer.Delete(index, length);
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

    public string Substring(int start, int length)
    {
        _stringBuilder.Clear();
        _textBuffer.GetTextSegment(start, length, _stringBuilder);
        return _stringBuilder.ToString();
    }

    public void GetTextSegment(int start, int length, StringBuilder output)
    {
        _textBuffer.GetTextSegment(start, length, output);
    }

    public void ReplaceAll(string newText)
    {
        _textBuffer.ReplaceAll(newText);
    }

    public void Iterate(Action<char> action)
    {
        _textBuffer.Iterate(action);
    }

    public ReadOnlySpan<char> AsSpan(int start, int length)
    {
        _stringBuilder.Clear();
        _textBuffer.GetTextSegment(start, length, _stringBuilder);
        return _stringBuilder.ToString().AsSpan();
    }

    public void AppendTo(StringBuilder sb)
    {
        _textBuffer.Iterate(c => sb.Append(c));
    }
}