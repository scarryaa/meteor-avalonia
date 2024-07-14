using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace meteor.Views.Interfaces;

public interface ILspClient
{
    event EventHandler<string>? LogReceived;
    Task InitializeAsync();
    Task<CompletionList> RequestCompletionAsync(string documentUri, Position position, CompletionContext context);
    Task<Hover> RequestHoverAsync(string documentUri, Position position);
    Task SendDidChangeAsync(string documentUri, string text, int version);
    Task SendDidOpenAsync(string documentUri, string languageId, int version, string text);
    void Dispose();
}