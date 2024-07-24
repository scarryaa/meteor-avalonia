using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class TextBufferService : ITextBufferService
{
    public string GetContent()
    {
        return TextBuffer.GetRopeContent();
    }

    public void InsertText(int position, string text)
    {
        TextBuffer.insert_text(position, text);
    }

    public void DeleteText(int position, int length)
    {
        TextBuffer.delete_text(position, length);
    }

    public int GetLength()
    {
        return TextBuffer.get_rope_length();
    }
}