using meteor.Interfaces;

namespace meteor.Models;

public class TextBufferFactory : ITextBufferFactory
{
    public ITextBuffer Create()
    {
        return new TextBuffer();
    }
}