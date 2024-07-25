using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using meteor.Core.Interfaces.Services;
using meteor.Core.Services;
using meteor.UI.Services;
using meteor.UI.ViewModels;
using meteor.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace meteor;

public class App : Application
{
    private IServiceProvider? Services { get; set; }

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

            BindingPlugins.DataValidators.RemoveAt(0);

            var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();
            var editorViewModel = Services.GetRequiredService<EditorViewModel>();
            var textMeasurer = Services.GetRequiredService<ITextMeasurer>();

            desktop.MainWindow = new MainWindow(mainWindowViewModel, editorViewModel,
                textMeasurer);

            var clipboardManager = Services.GetRequiredService<IClipboardManager>();
            if (clipboardManager is ClipboardManager cm) cm.TopLevelRef = desktop.MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ICursorManager, CursorManager>();
        services.AddSingleton<IInputManager, InputManager>();
        services.AddSingleton<ITextMeasurer, AvaloniaTextMeasurer>();
        services.AddTransient<ITextBufferService, TextBufferService>();
        services.AddSingleton<IClipboardManager, ClipboardManager>();
        services.AddTransient<ITextMeasurer, AvaloniaTextMeasurer>();

        // ViewModels
        services.AddTransient<EditorViewModel>();
        services.AddTransient<MainWindowViewModel>();
    }
}