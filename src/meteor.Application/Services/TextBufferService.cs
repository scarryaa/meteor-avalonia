using meteor.Application.Interfaces;
using meteor.Core.Entities;
using meteor.Core.Interfaces;

namespace meteor.Application.Services;

public class TextBufferService : ITextBufferService
{
    private readonly ITextBuffer _textBuffer;

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

    public string Substring(int start, int length)
    {
        return _textBuffer.Substring(start, length);
    }

    public string GetText()
    {
        return _textBuffer.GetText();
    }

    public string GetText(int start, int length)
    {
        return _textBuffer.GetText(start, length);
    }

    public void ReplaceAll(string newText)
    {
        _textBuffer.ReplaceAll(newText);
    }

    public void Iterate(Action<char> action)
    {
        _textBuffer.Iterate(action);
    }
}