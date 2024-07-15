using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using meteor.Views.Interfaces;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StreamJsonRpc;
using CompletionContext = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionContext;
using CompletionItem = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionItem;
using CompletionItemKind = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionItemKind;
using CompletionList = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionList;
using CompletionParams = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionParams;
using ConfigurationParams = Microsoft.VisualStudio.LanguageServer.Protocol.ConfigurationParams;
using DidChangeTextDocumentParams = Microsoft.VisualStudio.LanguageServer.Protocol.DidChangeTextDocumentParams;
using DidOpenTextDocumentParams = Microsoft.VisualStudio.LanguageServer.Protocol.DidOpenTextDocumentParams;
using Hover = Microsoft.VisualStudio.LanguageServer.Protocol.Hover;
using InitializeParams = Microsoft.VisualStudio.LanguageServer.Protocol.InitializeParams;
using InitializeResult = Microsoft.VisualStudio.LanguageServer.Protocol.InitializeResult;
using MarkupKind = Microsoft.VisualStudio.LanguageServer.Protocol.MarkupKind;
using Position = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using Registration = Microsoft.VisualStudio.LanguageServer.Protocol.Registration;
using RegistrationParams = Microsoft.VisualStudio.LanguageServer.Protocol.RegistrationParams;
using ResourceOperationKind = Microsoft.VisualStudio.LanguageServer.Protocol.ResourceOperationKind;
using ShowMessageParams = Microsoft.VisualStudio.LanguageServer.Protocol.ShowMessageParams;
using TextDocumentContentChangeEvent = Microsoft.VisualStudio.LanguageServer.Protocol.TextDocumentContentChangeEvent;
using TextDocumentIdentifier = Microsoft.VisualStudio.LanguageServer.Protocol.TextDocumentIdentifier;
using TextDocumentItem = Microsoft.VisualStudio.LanguageServer.Protocol.TextDocumentItem;
using VersionedTextDocumentIdentifier = Microsoft.VisualStudio.LanguageServer.Protocol.VersionedTextDocumentIdentifier;

namespace meteor.Views.Services;

public class LspClient : IDisposable, ILspClient
{
    private readonly JsonRpc _rpc;
    private readonly Process _process;
    private readonly string _projectRootPath;
    private bool _isInitialized;
    private readonly Dictionary<string, Registration> _registeredCapabilities = new();
    private readonly object _disposeLock = new();
    private bool _disposed;
    private int _initializationRetryCount = 0;
    private const int MaxInitializationRetries = 3;

    public event EventHandler<string>? LogReceived;

    public LspClient(Process process, string projectRootPath)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        _projectRootPath = projectRootPath ?? throw new ArgumentNullException(nameof(projectRootPath));

        try
        {
            var writer = process.StandardInput.BaseStream;
            var reader = process.StandardOutput.BaseStream;

            _rpc = new JsonRpc(new HeaderDelimitedMessageHandler(writer, reader));
            _rpc.TraceSource.Switch.Level = SourceLevels.Verbose;
            _rpc.TraceSource.Listeners.Add(new ConsoleTraceListener());
            RegisterHandlers();
            _rpc.StartListening();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing LspClient: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private void RegisterHandlers()
    {
        _rpc.AddLocalRpcMethod("window/showMessage", new Action<JToken>(OnShowMessage));
        _rpc.AddLocalRpcMethod("client/registerCapability", new Func<JToken, Task>(OnRegisterCapabilityAsync));
        _rpc.AddLocalRpcMethod("workspace/configuration", new Func<JToken, Task<JToken>>(OnConfigurationAsync));
        _rpc.AddLocalRpcMethod("window/workDoneProgress/create", new Func<JToken, Task>(OnWorkDoneProgressCreateAsync));
        _rpc.AddLocalRpcMethod("$/progress", new Action<JToken>(OnProgress));
    }

    private void OnShowMessage(JToken @params)
    {
        var showMessageParams = @params.ToObject<ShowMessageParams>();
        Console.WriteLine($"Server message: {showMessageParams.MessageType} - {showMessageParams.Message}");
    }

    private async Task OnRegisterCapabilityAsync(JToken @params)
    {
        var registrationParams = @params.ToObject<RegistrationParams>();
        foreach (var registration in registrationParams.Registrations)
        {
            Console.WriteLine($"Registering capability: {registration.Method} (ID: {registration.Id})");
            _registeredCapabilities[registration.Id] = registration;
        }
    }

    private async Task<JToken> OnConfigurationAsync(JToken @params)
    {
        var configurationParams = @params.ToObject<ConfigurationParams>();
        var configurations = new List<object>();
        foreach (var item in configurationParams.Items)
        {
            Console.WriteLine($"Configuration requested for {item.ScopeUri}, section: {item.Section}");
            configurations.Add(new Dictionary<string, object>
            {
                ["enable"] = true,
                ["diagnostics"] = true,
                ["formatting"] = true
            });
        }

        return JToken.FromObject(configurations);
    }

    private async Task OnWorkDoneProgressCreateAsync(JToken @params)
    {
        var workDoneProgressCreateParams = @params.ToObject<WorkDoneProgressCreateParams>();
        Console.WriteLine($"Work Done Progress created with token: {workDoneProgressCreateParams.Token}");
    }

    private void OnProgress(JToken @params)
    {
        var token = @params["token"].ToString();
        var value = @params["value"];
        Console.WriteLine($"Progress: Token: {token}, Value: {value}");
    }

    public async Task InitializeAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LspClient));

        while (_initializationRetryCount < MaxInitializationRetries)
        {
            try
            {
                Console.WriteLine("Initializing LSP client...");
                var initializeParams = new InitializeParams
                {
                    ProcessId = Process.GetCurrentProcess().Id,
                    RootUri = new Uri("file:///" + _projectRootPath.Replace("\\", "/")),
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
                                    ValueSet = Enum.GetValues(typeof(CompletionItemKind)).Cast<CompletionItemKind>()
                                        .ToArray()
                                },
                                CompletionListSetting = new CompletionListSetting
                                {
                                    ItemDefaults = new[]
                                    {
                                        "documentation: Default documentation for the completion item.",
                                        "detail: Default detail about the completion item.",
                                        "insertTextFormat: PlainText"
                                    }
                                }
                            },
                            Synchronization = new SynchronizationSetting
                            {
                                DynamicRegistration = true,
                                WillSave = true,
                                WillSaveWaitUntil = true,
                                DidSave = true
                            },
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
                            DidChangeConfiguration = new DynamicRegistrationSetting { DynamicRegistration = true },
                            DidChangeWatchedFiles = new DynamicRegistrationSetting { DynamicRegistration = true },
                            ApplyEdit = true,
                            WorkspaceEdit = new WorkspaceEditSetting
                            {
                                DocumentChanges = true,
                                ResourceOperations = new[]
                                {
                                    ResourceOperationKind.Create, ResourceOperationKind.Rename, ResourceOperationKind.Delete
                                }
                            },
                            Symbol = new SymbolSetting { DynamicRegistration = true },
                            ExecuteCommand = new DynamicRegistrationSetting { DynamicRegistration = true }
                        }
                    },
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

                Console.WriteLine("Sending initialize request...");
                var response = await _rpc.InvokeWithParameterObjectAsync<InitializeResult>("initialize", initializeParams);
                Console.WriteLine($"Server capabilities: {JToken.FromObject(response.Capabilities)}");

                Console.WriteLine("Sending initialized notification...");
                await _rpc.NotifyAsync("initialized", new object());
                Console.WriteLine("LSP client initialization complete.");
                _isInitialized = true;
                return;
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Error during LSP initialization: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                LogReceived?.Invoke(this, $"Error during LSP initialization: {ex.Message}");
                _initializationRetryCount++;
                await Task.Delay(1000); // Retry after a delay
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during LSP initialization: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                LogReceived?.Invoke(this, $"Error during LSP initialization: {ex.Message}");
                throw;
            }
        }

        throw new InvalidOperationException("Failed to initialize LSP client after multiple retries.");
    }

    public async Task SendDidOpenAsync(string documentUri, string languageId, int version, string text)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LspClient));
        if (!_isInitialized) throw new InvalidOperationException("LSP client is not initialized.");

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

        Console.WriteLine($"Sending didOpen notification for {documentUri}");
        Console.WriteLine($"DidOpen parameters: {JToken.FromObject(parameters)}");
        await _rpc.NotifyWithParameterObjectAsync("textDocument/didOpen", parameters);
        Console.WriteLine("didOpen notification sent successfully");
    }

    public Task<Hover> RequestHoverAsync(string documentUri, Position position)
    {
        return Task.FromResult<Hover>(null);
    }

    public async Task SendDidChangeAsync(string documentUri, string text, int version)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LspClient));
        if (!_isInitialized) throw new InvalidOperationException("LSP client is not initialized.");

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

        Console.WriteLine($"Sending didChange notification for {documentUri}");
        Console.WriteLine($"DidChange parameters: {JToken.FromObject(parameters)}");
        Console.WriteLine($"Full document content:\n{text}");
        await _rpc.NotifyWithParameterObjectAsync("textDocument/didChange", parameters);
        Console.WriteLine("didChange notification sent successfully");
    }

    public async Task<CompletionList> RequestCompletionAsync(string documentUri, Position position,
        CompletionContext context)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LspClient));
        if (!_isInitialized) throw new InvalidOperationException("LSP client is not initialized.");

        var adjustedPosition = new Position
        {
            Line = position.Line,
            Character = position.Character
        };

        var parameters = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(documentUri) },
            Position = position,
            Context = context
        };

        try
        {
            Console.WriteLine(
                $"Requesting completion for {documentUri} at position {adjustedPosition.Line}:{adjustedPosition.Character}");
            Console.WriteLine($"Completion parameters: {JToken.FromObject(parameters)}");

            var result =
                await _rpc.InvokeWithParameterObjectAsync<CompletionList>("textDocument/completion", parameters);

            if (result == null)
            {
                Console.WriteLine("Received null completion result from language server");
                return new CompletionList { IsIncomplete = false, Items = Array.Empty<CompletionItem>() };
            }

            Console.WriteLine($"Received completion response with {result.Items?.Length ?? 0} items.");
            Console.WriteLine($"Completion result: {JToken.FromObject(result)}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting completion: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new CompletionList { IsIncomplete = false, Items = Array.Empty<CompletionItem>() };
        }
    }

    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed) return;
            
            Task.Delay(1000).Wait();

            _rpc.Dispose();
            if (!_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit();
            }

            _process.Dispose();
            _disposed = true;
        }
    }
}