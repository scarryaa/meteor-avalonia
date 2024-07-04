using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using meteor.Interfaces;
using meteor.Models;
using meteor.Services;
using meteor.ViewModels;
using meteor.Views;
using meteor.Views.Services;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("tests")]

namespace meteor;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; }

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
        // Register services
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

        // Register text buffer related services
        services.AddSingleton<ITextBufferFactory, TextBufferFactory>();
        services.AddSingleton<IRope, Rope>();
        services.AddSingleton<ITextBuffer, TextBuffer>();

        // Register file system watcher factory
        services.AddSingleton<IFileSystemWatcherFactory, FileSystemWatcherFactory>();

        // Register undo/redo manager
        services.AddSingleton<IUndoRedoManager<TextState>, UndoRedoManager<TextState>>(provider =>
        {
            var initialState = new TextState("", 0);
            return new UndoRedoManager<TextState>(initialState);
        });

        // Register main window and clipboard service
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IClipboardService>(provider =>
        {
            var mainWindow = provider.GetRequiredService<MainWindow>();
            return new ClipboardService(mainWindow);
        });

        // Register main window provider
        services.AddSingleton<IMainWindowProvider, MainWindowProvider>();

        // Register theme service with the application instance
        services.AddSingleton<IThemeService>(provider => new ThemeService(this));

        services.AddTransient<RenderManager>();
        services.AddTransient<InputManager>();
        services.AddTransient<ScrollManager>();
        services.AddTransient<ClipboardManager>();
        services.AddTransient<CursorManager>();
        services.AddTransient<SelectionManager>();
        services.AddTransient<LineManager>();
        services.AddTransient<TextManipulator>();
    }

    internal static void SetServiceProviderForTesting(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}
