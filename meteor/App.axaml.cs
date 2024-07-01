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

            desktop.MainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ICursorPositionService, CursorPositionService>();
        services.AddSingleton<FontPropertiesViewModel>();
        services.AddSingleton<LineCountViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<StatusPaneViewModel>();
        services.AddTransient<TextEditorViewModel>();
        services.AddTransient<FileExplorerViewModel>();
        services.AddTransient<TitleBarViewModel>();
        services.AddTransient<ScrollableTextEditorViewModel>();

        services.AddSingleton<ITextBuffer, TextBuffer>();
        services.AddSingleton<IFileSystemWatcherFactory, FileSystemWatcherFactory>();

        services.AddSingleton<IUndoRedoManager<TextState>, UndoRedoManager<TextState>>(provider =>
        {
            var initialState = new TextState("", 0);
            return new UndoRedoManager<TextState>(initialState);
        });
    }

    internal static void SetServiceProviderForTesting(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}