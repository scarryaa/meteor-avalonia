using meteor.Core.Interfaces;
using meteor.Core.Models;
using Microsoft.Extensions.Logging;

namespace meteor.Core.Services;

public class TextBufferFactory : ITextBufferFactory
{
    private readonly ILogger<Rope> _logger;
    private readonly IRope _rope;

    public TextBufferFactory(ILogger<Rope> logger, IRope rope)
    {
        _logger = logger;
        _rope = rope;
    }

    public ITextBuffer Create()
    {
        return new TextBuffer(new Rope("", _logger), _logger);
    }
}