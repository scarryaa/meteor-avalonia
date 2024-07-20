using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using meteor.Application.Services;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Services;
using meteor.Infrastructure.Data;
using meteor.UI.Factories;
using meteor.UI.Interfaces;
using meteor.UI.Resources;
using meteor.UI.Services;
using meteor.UI.ViewModels;
using meteor.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.UI;

public class App : Avalonia.Application
{
    private IServiceProvider? _serviceProvider;
    private static IThemeManager? ThemeManager { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        ThemeManager = new ThemeManager(this);

        ThemeManager.AddTheme("Dark", new DarkTheme());
        ThemeManager.AddTheme("Light", new LightTheme());

        ThemeManager.SetTheme("Light");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IRope, Rope>(sp => new Rope(""));
        services.AddSingleton<IClipboardService>(sp =>
            new AvaloniaClipboardService(() => GetMainWindow(sp))
        );

        services.AddSingleton<ITabService, TabService>();
        services.AddTransient<ITextBufferService, TextBufferService>();
        services.AddSingleton<ISyntaxHighlighter, SyntaxHighlighter>();
        services.AddSingleton<ICursorService, CursorService>();
        services.AddSingleton<ISelectionService, SelectionService>();
        services.AddSingleton<ITextAnalysisService, TextAnalysisService>();
        services.AddSingleton<IInputService, InputService>();
        services.AddSingleton<ITextMeasurer>(sp =>
            new AvaloniaTextMeasurer(
                new Typeface("Consolas"),
                13
            ));
        services.AddSingleton<IEditorSizeCalculator, AvaloniaEditorSizeCalculator>();

        services.AddTransient<IEditorViewModel, EditorViewModel>();
        services.AddTransient<ITabItemViewModel, TabItemViewModel>();
        services.AddSingleton<ITabViewModel, TabViewModel>();
        services.AddTransient<IMainWindowViewModel, MainWindowViewModel>();

        services.AddSingleton<ITabService, TabService>();
        services.AddTransient<IEditorViewModelFactory>(sp =>
            new EditorViewModelFactory(sp));
        services.AddSingleton<ICommandFactory, CommandFactory>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<EditorView>(provider =>
        {
            var viewModel = provider.GetService<IEditorViewModel>();
            var editorView = new EditorView
            {
                DataContext = viewModel
            };
            return editorView;
        });
    }

    private static Window GetMainWindow(IServiceProvider serviceProvider)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow ?? serviceProvider.GetRequiredService<MainWindow>();
        throw new InvalidOperationException("Unable to resolve MainWindow.");
    }
}