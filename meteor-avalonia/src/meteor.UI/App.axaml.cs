using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Config;
using meteor.Core.Services;
using meteor.UI.Factories;
using meteor.UI.Interfaces;
using meteor.UI.Services;
using meteor.UI.ViewModels;
using meteor.UI.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.UI;

public class App : Application
{
    private IServiceProvider? _serviceProvider;
    private static IThemeManager? ThemeManager { get; set; }
    public static IConfiguration Configuration { get; private set; }

    public IServiceProvider ServiceProvider =>
        _serviceProvider ?? throw new InvalidOperationException("Service provider not initialized.");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true)
            .AddEnvironmentVariables();

        Configuration = builder.Build();

        var services = new ServiceCollection();

        // Create and register ThemeManager before setting the theme
        ThemeManager = new ThemeManager(this);
        services.AddSingleton(ThemeManager);

        // Load themes and set the initial theme
        ThemeManager.LoadThemesFromJson("themes.json");
        ThemeManager.SetTheme("Light");

        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Bind the ThemeConfig section
        var themeConfig = new ThemeConfig();
        Configuration.GetSection("ThemeConfig").Bind(themeConfig);
        services.AddSingleton(themeConfig);

        services.AddSingleton<IRope, Rope>(sp => new Rope(""));
        services.AddSingleton<IClipboardService>(sp => new AvaloniaClipboardService(() => GetMainWindow(sp)));

        services.AddSingleton<IScrollManager, ScrollManager>();
        services.AddSingleton<ITabService, TabService>();
        services.AddTransient<ITextBufferService, TextBufferService>();
        services.AddSingleton<ISyntaxHighlighter, SyntaxHighlighter>();     
        services.AddSingleton<ICursorService, CursorService>();
        services.AddSingleton<ISelectionService, SelectionService>();
        services.AddSingleton<ITextAnalysisService, TextAnalysisService>();
        services.AddSingleton<IInputService, InputService>();

        services.AddSingleton<ITextMeasurer>(sp =>
        {
            var themeManager = sp.GetService<IThemeManager>();
            var baseTheme = themeManager.GetBaseTheme();
            if (!baseTheme.TryGetValue("TextEditorFontFamily", out var fontFamilyValue) ||
                !baseTheme.TryGetValue("TextEditorFontSize", out var fontSizeValue))
                throw new InvalidOperationException("FontFamily or FontSize not found in the theme.");

            var themeConfig = sp.GetRequiredService<ThemeConfig>();
            var fontFamily = new FontFamily(new Uri(themeConfig.FontFamilyUri), fontFamilyValue.ToString());
            var fontSize = Convert.ToDouble(fontSizeValue);

            return new AvaloniaTextMeasurer(new Typeface(fontFamily), fontSize);
        });

        services.AddSingleton<IEditorSizeCalculator, AvaloniaEditorSizeCalculator>();

        services.AddTransient<IGutterViewModel, GutterViewModel>();
        services.AddTransient<IEditorViewModel, EditorViewModel>();
        services.AddTransient<ITabItemViewModel, TabItemViewModel>();
        services.AddSingleton<ITabViewModel, TabViewModel>();
        services.AddTransient<IMainWindowViewModel, MainWindowViewModel>();

        services.AddSingleton<ITabService, TabService>();
        services.AddTransient<IEditorViewModelFactory>(sp => new EditorViewModelFactory(sp));
        services.AddSingleton<ICommandFactory, CommandFactory>();

        services.AddSingleton<MainWindow>();
        services.AddTransient<GutterView>();
        services.AddTransient<EditorView>();

        services.AddTransient<EditorViewModelServiceContainer>(sp => new EditorViewModelServiceContainer(
            sp.GetRequiredService<ITextBufferService>(),
            sp.GetRequiredService<ITabService>(),
            sp.GetRequiredService<ISyntaxHighlighter>(),
            sp.GetRequiredService<ISelectionService>(),
            sp.GetRequiredService<IInputService>(),
            sp.GetRequiredService<ICursorService>(),
            sp.GetRequiredService<IEditorSizeCalculator>()
        ));
    }

    private static Window GetMainWindow(IServiceProvider serviceProvider)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow ?? serviceProvider.GetRequiredService<MainWindow>();
        throw new InvalidOperationException("Unable to resolve MainWindow.");
    }
}