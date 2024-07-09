using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;
using ClientCapabilities = Microsoft.VisualStudio.LanguageServer.Protocol.ClientCapabilities;
using CompletionContext = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionContext;
using CompletionItemKind = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionItemKind;
using CompletionList = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionList;
using CompletionParams = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionParams;
using DidChangeTextDocumentParams = Microsoft.VisualStudio.LanguageServer.Protocol.DidChangeTextDocumentParams;
using DidOpenTextDocumentParams = Microsoft.VisualStudio.LanguageServer.Protocol.DidOpenTextDocumentParams;
using Hover = Microsoft.VisualStudio.LanguageServer.Protocol.Hover;
using InitializeParams = Microsoft.VisualStudio.LanguageServer.Protocol.InitializeParams;
using InitializeResult = OmniSharp.Extensions.LanguageServer.Protocol.Models.InitializeResult;
using MarkupKind = Microsoft.VisualStudio.LanguageServer.Protocol.MarkupKind;
using Position = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using TextDocumentClientCapabilities = Microsoft.VisualStudio.LanguageServer.Protocol.TextDocumentClientCapabilities;
using TextDocumentContentChangeEvent = Microsoft.VisualStudio.LanguageServer.Protocol.TextDocumentContentChangeEvent;
using TextDocumentIdentifier = Microsoft.VisualStudio.LanguageServer.Protocol.TextDocumentIdentifier;
using TextDocumentItem = Microsoft.VisualStudio.LanguageServer.Protocol.TextDocumentItem;
using TextDocumentPositionParams = Microsoft.VisualStudio.LanguageServer.Protocol.TextDocumentPositionParams;
using VersionedTextDocumentIdentifier = Microsoft.VisualStudio.LanguageServer.Protocol.VersionedTextDocumentIdentifier;
using WorkspaceClientCapabilities = Microsoft.VisualStudio.LanguageServer.Protocol.WorkspaceClientCapabilities;

public class LspClient : IDisposable
{
    private readonly JsonRpc _rpc;
    private readonly Process _process;
    private readonly Stream _outputStream;
    private readonly Stream _inputStream;

    public event EventHandler<string>? LogReceived;

    public LspClient(string workspacePath, string languageServerPath, string languageServerArgs)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = languageServerPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = languageServerArgs
        };

        _process = new Process { StartInfo = processStartInfo };
        _process.Start();

        // Capture standard error output
        _process.ErrorDataReceived += (sender, args) => Console.WriteLine($"LSP Error: {args.Data}");
        _process.BeginErrorReadLine();

        _inputStream = _process.StandardOutput.BaseStream;
        _outputStream = _process.StandardInput.BaseStream;

        // Use the HeaderDelimitedMessageHandler which handles framing
        _rpc = new JsonRpc(new HeaderDelimitedMessageHandler(_outputStream, _inputStream));
        _rpc.StartListening();
    }

    public async Task InitializeAsync()
    {
        var initializeParams = new InitializeParams
        {
            ProcessId = Process.GetCurrentProcess().Id,
            RootUri = new Uri("file:///" + Directory.GetCurrentDirectory().Replace("\\", "/")),
            Capabilities = new ClientCapabilities
            {
                TextDocument = new TextDocumentClientCapabilities
                {
                    Completion = new CompletionSetting
                    {
                        DynamicRegistration = true,
                        ContextSupport = true,
                        CompletionItem = new CompletionItemSetting
                        {
                            SnippetSupport = true,
                            CommitCharactersSupport = true,
                            DocumentationFormat = new[] { MarkupKind.Markdown, MarkupKind.PlainText }
                        },
                        CompletionItemKind = new CompletionItemKindSetting
                        {
                            ValueSet = Enum.GetValues(typeof(CompletionItemKind)).Cast<CompletionItemKind>().ToArray()
                        }
                    },
                    Synchronization = new SynchronizationSetting
                    {
                        DynamicRegistration = true,
                        WillSave = true,
                        WillSaveWaitUntil = true,
                        DidSave = true
                    },
                    // Add TypeScript-specific capabilities
                    CodeAction = new CodeActionSetting { DynamicRegistration = true },
                    Definition = new DynamicRegistrationSetting { DynamicRegistration = true },
                    TypeDefinition = new DocumentSymbolSetting { DynamicRegistration = true },
                    Implementation = new DocumentSymbolSetting { DynamicRegistration = true },
                    References = new DynamicRegistrationSetting { DynamicRegistration = true },
                    DocumentSymbol = new DocumentSymbolSetting { DynamicRegistration = true },
                    Rename = new DynamicRegistrationSetting { DynamicRegistration = true },
                    SignatureHelp = new SignatureHelpSetting { DynamicRegistration = true }
                },
                Workspace = new WorkspaceClientCapabilities
                {
                    ApplyEdit = true,
                    WorkspaceEdit = new WorkspaceEditSetting
                    {
                        DocumentChanges = true
                    },
                    DidChangeConfiguration = new DynamicRegistrationSetting
                    {
                        DynamicRegistration = true
                    },
                    DidChangeWatchedFiles = new DynamicRegistrationSetting
                    {
                        DynamicRegistration = true
                    },
                    Symbol = new SymbolSetting { DynamicRegistration = true }
                }
            },
            // Add TypeScript-specific initialization options
            InitializationOptions = new
            {
                provideFormatter = true,
                provideRefactors = true,
                maxTsServerMemory = 3072,
                disableAutomaticTypingAcquisition = false,
                tsserver = new
                {
                    logVerbosity = "verbose",
                    debugPort = 5000,
                    useSeparateSyntaxServer = true,
                    useSyntaxServer = "auto"
                },
                preferences = new
                {
                    includeCompletionsForModuleExports = true,
                    includeCompletionsWithObjectLiteralMethodSnippets = true,
                    includeAutomaticOptionalChainCompletions = true,
                    includeCompletionsWithSnippetText = true,
                    allowIncompleteCompletions = false
                }
            }
        };

        try
        {
            var response = await _rpc.InvokeWithParameterObjectAsync<InitializeResult>("initialize", initializeParams);
            LogReceived?.Invoke(this, $"Initialized with server capabilities: {response.Capabilities}");
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke(this, $"Error during initialization: {ex.Message}");
            throw;
        }
    }

    public async Task SendDidOpenAsync(string documentUri, string languageId, int version, string text)
    {
        var parameters = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = new Uri(documentUri),
                LanguageId = languageId,
                Version = version,
                Text = text
            }
        };

        try
        {
            await _rpc.NotifyWithParameterObjectAsync("textDocument/didOpen", parameters);
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke(this, $"Error sending didOpen notification: {ex.Message}");
            throw;
        }
    }

    public async Task<CompletionList> RequestCompletionAsync(string documentUri, Position position,
        CompletionContext context)
    {
        var parameters = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(documentUri) },
            Position = position,
            Context = context
        };

        try
        {
            Console.WriteLine(
                $"Sending completion request for {documentUri} at position {position.Line}:{position.Character}");
            Console.WriteLine($"Request parameters: {JsonSerializer.Serialize(parameters)}");

            var result =
                await _rpc.InvokeWithParameterObjectAsync<CompletionList>("textDocument/completion", parameters);

            Console.WriteLine($"Received completion response with {result.Items?.Length ?? 0} items.");
            Console.WriteLine($"Response: {JsonSerializer.Serialize(result)}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting completion: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new CompletionList { IsIncomplete = false, Items = Array.Empty<CompletionItem>() };
        }
    }

    public async Task<Hover> RequestHoverAsync(string documentUri, Position position)
    {
        var parameters = new TextDocumentPositionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(documentUri) },
            Position = position
        };

        try
        {
            return await _rpc.InvokeWithParameterObjectAsync<Hover>("textDocument/hover", parameters);
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke(this, $"Error requesting hover: {ex.Message}");
            throw;
        }
    }

    public async Task SendDidChangeAsync(string documentUri, string text, int version)
    {
        var parameters = new DidChangeTextDocumentParams
        {
            TextDocument = new VersionedTextDocumentIdentifier
            {
                Uri = new Uri(documentUri),
                Version = version
            },
            ContentChanges = new[]
            {
                new TextDocumentContentChangeEvent
                {
                    Text = text
                }
            }
        };

        try
        {
            await _rpc.NotifyWithParameterObjectAsync("textDocument/didChange", parameters);
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke(this, $"Error sending didChange notification: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _rpc.Dispose();
        _process.Dispose();
    }
}