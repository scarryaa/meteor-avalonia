using System.Diagnostics;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace meteor.languageserver;

public class LanguageServerClient : IDisposable
{
    private readonly JsonRpc _rpc;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Process _languageServerProcess;
    private readonly Stream _inputStream;
    private readonly Stream _outputStream;

    public LanguageServerClient(string languageServerPath, string languageServerArgs)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _languageServerProcess =
            StartLanguageServer(languageServerPath, languageServerArgs, out _inputStream, out _outputStream);
        _rpc = JsonRpc.Attach(_outputStream, _inputStream, this);
    }

    private Process StartLanguageServer(string path, string args, out Stream inputStream, out Stream outputStream)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = path,
            Arguments = args,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = processStartInfo };
        process.Start();

        inputStream = process.StandardOutput.BaseStream;
        outputStream = process.StandardInput.BaseStream;

        return process;
    }

    public async Task InitializeAsync(string rootPath)
    {
        var initializeParams = new InitializeParams
        {
            RootPath = rootPath,
            Capabilities = new ClientCapabilities()
        };

        try
        {
            var result = await _rpc.InvokeWithCancellationAsync<InitializeResult>(
                "initialize",
                new object[] { initializeParams },
                _cancellationTokenSource.Token
            );
            Console.WriteLine("Language server initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize language server: {ex.Message}");
        }
    }

    public async Task<Hover> RequestHoverAsync(string documentUri, Position position)
    {
        var hoverParams = new TextDocumentPositionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(documentUri) },
            Position = position
        };

        try
        {
            return await _rpc.InvokeWithCancellationAsync<Hover>(
                "textDocument/hover",
                new object[] { hoverParams },
                _cancellationTokenSource.Token
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to request hover: {ex.Message}");
            return null;
        }
    }

    public async Task<CompletionList> RequestCompletionAsync(string documentUri, Position position)
    {
        var completionParams = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(documentUri) },
            Position = position,
            Context = new CompletionContext { TriggerKind = CompletionTriggerKind.Invoked }
        };

        try
        {
            return await _rpc.InvokeWithCancellationAsync<CompletionList>(
                "textDocument/completion",
                new object[] { completionParams },
                _cancellationTokenSource.Token
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to request completion: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _rpc.Dispose();
        _cancellationTokenSource.Dispose();
        _inputStream.Dispose();
        _outputStream.Dispose();
        if (!_languageServerProcess.HasExited) _languageServerProcess.Kill();
        _languageServerProcess.Dispose();
    }
}