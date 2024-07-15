using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using meteor.App.Rendering;
using meteor.App.Services;
using meteor.App.Views;
using meteor.Core.Contexts;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Interfaces.Contexts;
using meteor.Core.Interfaces.Events;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Interfaces.Resources;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Commands;
using meteor.Core.Models.Events;
using meteor.Core.Models.Resources;
using meteor.Core.Services;
using meteor.Services;
using meteor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FontStyle = meteor.Core.Models.Rendering.FontStyle;
using FontWeight = meteor.Core.Models.Rendering.FontWeight;

namespace meteor.App;

public class App : Application
{
    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }

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
            Services = services.BuildServiceProvider();

            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register logging
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Debug);
        });
        
        // Register services
        services.AddSingleton<ITextBuffer, TextBuffer>();
        services.AddSingleton<IClipboardService>(_ =>
        {
            var mainWindow = ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)
                ?.MainWindow;
            var topLevel = mainWindow != null ? TopLevel.GetTopLevel(mainWindow) : null;
            return new AvaloniaClipboardService(topLevel!);
        });
        services.AddSingleton<IImageFactory, AvaloniaImageFactory>();
        services.AddSingleton<ICacheManager, CacheManager>();
        services.AddSingleton<ITextMeasurer, AvaloniaTextMeasurer>();
        services.AddSingleton<IUndoRedoManager<ITextBuffer>, UndoRedoManager<ITextBuffer>>();
        services.AddSingleton<ICursorManager, CursorManager>();
        services.AddSingleton<ISelectionHandler, SelectionHandler>();
        services.AddSingleton<IWordBoundaryService, WordBoundaryService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ICursorPositionService, CursorPositionService>();
        services.AddSingleton<ISyntaxHighlighter, SyntaxHighlighter>();
        services.AddSingleton<ITextBufferFactory, TextBufferFactory>();
        services.AddSingleton<IDialogService>(sp =>
        {
            var mainWindow = ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)
                ?.MainWindow;
            return new DialogService(mainWindow);
        });

        // Register IApplicationResourceProvider
        services.AddSingleton<IApplicationResourceProvider, ApplicationResourceProvider>();

        // Register ITextEditorContext
        services.AddSingleton<ITextEditorContext>(sp =>
        {
            var textEditorViewModel = sp.GetRequiredService<ITextEditorViewModel>();

            return new TextEditorContext(
                sp.GetRequiredService<ITextMeasurer>()
                    .MeasureHeight("Aj", 13, "Consolas"),
                new BrushAdapter(new SolidColorBrush(Colors.White)),
                new BrushAdapter(new SolidColorBrush(Colors.LightGray)),
                new BrushAdapter(new SolidColorBrush(Colors.LightBlue)),
                new BrushAdapter(new SolidColorBrush(Colors.Black)),
                2,
                1,
                new FontFamilyAdapter(new FontFamily("Consolas")),
                13,
                new BrushAdapter(new SolidColorBrush(Colors.Black)),
                textEditorViewModel,
                FontStyle.Normal,
                FontWeight.Normal,
                new BrushAdapter(new SolidColorBrush(Colors.Black))
            );
        });

        // Register ViewModels
        services.AddTransient<ITextEditorViewModel, TextEditorViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<TextEditorViewModel>();
        services.AddTransient<ILineCountViewModel, LineCountViewModel>();
        services.AddTransient<IGutterViewModel, GutterViewModel>(sp =>
        {
            var cursorPositionService = sp.GetRequiredService<ICursorPositionService>();
            var lineCountViewModel = sp.GetRequiredService<ILineCountViewModel>();
            var textEditorViewModel =
                new Lazy<ITextEditorViewModel>(sp.GetRequiredService<ITextEditorViewModel>);
            var themeService = sp.GetRequiredService<IThemeService>();
            var gutterLogger = sp.GetRequiredService<ILogger<GutterViewModel>>();
            return new GutterViewModel(cursorPositionService, lineCountViewModel, textEditorViewModel, themeService,
                gutterLogger);
        });

        // Register other dependencies
        services.AddSingleton<TextEditorCommands>();
        services.AddSingleton<RenderManager>(sp =>
        {
            var syntaxHighlighterFactory =
                new Func<ISyntaxHighlighter>(sp.GetRequiredService<ISyntaxHighlighter>);
            return new RenderManager(sp.GetRequiredService<ITextEditorContext>(),
                sp.GetRequiredService<IThemeService>(), syntaxHighlighterFactory,
                sp.GetRequiredService<ITextMeasurer>(),
                sp.GetRequiredService<ILogger<RenderManager>>());
        });
        services.AddSingleton<ITextEditorCommands, TextEditorCommands>();
        services.AddSingleton<IInputManager, InputManager>();
        services.AddSingleton<InputManager>();
        services.AddSingleton<IEventAggregator, EventAggregator>();
    }
}
