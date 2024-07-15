using meteor.Core.Interfaces;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class TextBufferFactory : ITextBufferFactory
{
    public ITextBuffer Create()
    {
        return new TextBuffer();
    }
}