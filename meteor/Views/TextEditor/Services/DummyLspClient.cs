using System;
using System.Threading.Tasks;
using meteor.Views.Interfaces;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace meteor.Views.Services;

public class DummyLspClient : ILspClient
{
    public event EventHandler<string>? LogReceived;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task<CompletionList> RequestCompletionAsync(string documentUri, Position position, CompletionContext context)
    {
        return Task.FromResult(new CompletionList());
    }

    public Task<Hover> RequestHoverAsync(string documentUri, Position position)
    {
        return Task.FromResult(new Hover());
    }

    public Task SendDidChangeAsync(string documentUri, string text, int version)
    {
        return Task.CompletedTask;
    }

    public Task SendDidOpenAsync(string documentUri, string languageId, int version, string text)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // DO nothing
    }
}