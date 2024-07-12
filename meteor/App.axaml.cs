using System;
using System.IO;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using meteor.Interfaces;
using meteor.Models;
using meteor.Services;
using meteor.ViewModels;
using meteor.Views;
using meteor.Views.Interfaces;
using meteor.Views.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;

[assembly: InternalsVisibleTo("tests")]

namespace meteor;

public class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();
            ServiceLocator.SetLocatorProvider(ServiceProvider);

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(configure =>
        {
            configure.SetMinimumLevel(LogLevel.Debug);
            configure.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
            });
        });

        var languageServersConfigPath = Path.Combine(AppContext.BaseDirectory, "../../../languageServers.json");
        services.AddSingleton<LspClientFactory>(provider =>
        {
            try
            {
                return new LspClientFactory(languageServersConfigPath);
            }
            catch (Exception ex)
            {
                provider.GetRequiredService<ILogger<App>>().LogError(ex, "Failed to initialize LspClientFactory");
                throw;
            }
        });

        services.AddTransient<ILspClient>(provider =>
        {
            var factory = provider.GetRequiredService<LspClientFactory>();
            var filePath = "/path/to/source/file"; // Replace with actual file path
            return factory.GetOrCreateClient(filePath);
        });

        services.AddSingleton<Func<Task<TabViewModel>>>(serviceProvider => async () =>
        {
            var cursorPositionService = serviceProvider.GetRequiredService<ICursorPositionService>();
            var undoRedoManager = serviceProvider.GetRequiredService<IUndoRedoManager<TextState>>();
            var fileSystemWatcherFactory = serviceProvider.GetRequiredService<IFileSystemWatcherFactory>();
            var textBufferFactory = serviceProvider.GetRequiredService<ITextBufferFactory>();
            var fontPropertiesViewModel = serviceProvider.GetRequiredService<FontPropertiesViewModel>();
            var lineCountViewModel = serviceProvider.GetRequiredService<LineCountViewModel>();
            var clipboardService = serviceProvider.GetRequiredService<IClipboardService>();
            var autoSaveService = serviceProvider.GetRequiredService<IAutoSaveService>();
            var themeService = serviceProvider.GetRequiredService<IThemeService>();
            var languageClientFactory = serviceProvider.GetRequiredService<LspClientFactory>();

            var closeTabCommand = ReactiveCommand.Create<TabViewModel, Unit>(_ => Unit.Default);
            var closeOtherTabsCommand = ReactiveCommand.Create<TabViewModel, Unit>(_ => Unit.Default);
            var closeAllTabsCommand = ReactiveCommand.Create<TabViewModel, Unit>(_ => Unit.Default);

            return await TabViewModel.CreateAsync(
                languageClientFactory,
                cursorPositionService,
                undoRedoManager!,
                fileSystemWatcherFactory,
                textBufferFactory,
                fontPropertiesViewModel,
                lineCountViewModel,
                clipboardService,
                autoSaveService,
                themeService,
                closeTabCommand,
                closeOtherTabsCommand,
                closeAllTabsCommand)!;
        });

        // Register other services
        services.AddSingleton<IErrorLoggingService, ErrorLoggingService>();
        services.AddTransient<ITabFactory, TabFactory>();
        services.AddTransient<BottomPaneViewModel>();

        // ViewModels and Managers
        services.AddSingleton<ViewModelBase>();
        services.AddSingleton<ICursorPositionService, CursorPositionService>();
        services.AddSingleton<FontPropertiesViewModel>();
        services.AddSingleton<LineCountViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<StatusPaneViewModel>();
        services.AddTransient<TextEditorViewModel>();
        services.AddTransient<FileExplorerViewModel>();
        services.AddTransient<TitleBarViewModel>();
        services.AddTransient<ScrollableTextEditorViewModel>();
        services.AddSingleton<IAutoSaveService, AutoSaveService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ISyntaxHighlighter, SyntaxHighlighter>();

        // Text buffer related services
        services.AddSingleton<ITextBufferFactory, TextBufferFactory>();
        services.AddSingleton<IRope, Rope>();
        services.AddSingleton<ITextBuffer, TextBuffer>();

        // File system watcher factory
        services.AddSingleton<IFileSystemWatcherFactory, FileSystemWatcherFactory>();

        // Undo/redo manager
        services.AddSingleton<IUndoRedoManager<TextState>, UndoRedoManager<TextState>>(_ =>
        {
            var initialState = new TextState("", 0);
            return new UndoRedoManager<TextState>(initialState);
        });

        // Main window and clipboard service
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IClipboardService>(provider =>
        {
            var mainWindow = provider.GetRequiredService<MainWindow>();
            return new ClipboardService(mainWindow);
        });

        // Main window provider
        services.AddSingleton<IMainWindowProvider, MainWindowProvider>();

        // Theme service with the application instance
        services.AddSingleton<IThemeService>(_ => new ThemeService(this));

        // Register managers and services
        services.AddTransient<RenderManager>();
        services.AddTransient<InputManager>();
        services.AddTransient<ScrollManager>();
        services.AddTransient<ClipboardManager>();
        services.AddTransient<CursorManager>();
        services.AddTransient<SelectionManager>();
        services.AddTransient<LineManager>();
        services.AddTransient<TextManipulator>();
    }

    internal static void SetServiceProviderForTesting(IServiceProvider? serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}
