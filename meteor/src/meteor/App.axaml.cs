using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using meteor.Core.Config;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Commands;
using meteor.Core.Services;
using meteor.UI.Common.Converters;
using meteor.UI.Features.Editor.Factories;
using meteor.UI.Features.Editor.Interfaces;
using meteor.UI.Features.Editor.Services;
using meteor.UI.Features.Tabs.Factories;
using meteor.UI.Features.Tabs.ViewModels;
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
            var textMeasurer = Services.GetRequiredService<ITextMeasurer>();
            var config = Services.GetRequiredService<IEditorConfig>();
            var scrollManager = Services.GetRequiredService<IScrollManager>();
            var layoutManager = Services.GetRequiredService<IEditorLayoutManager>();
            var inputHandler = Services.GetRequiredService<IEditorInputHandler>();
            var pointerEventHandler = Services.GetRequiredService<IPointerEventHandler>();
            var tabService = Services.GetRequiredService<ITabService>();
            var themeManager = Services.GetRequiredService<IThemeManager>();

            IsActiveToBrushConverter.Initialize(themeManager);

            desktop.MainWindow = new MainWindow(mainWindowViewModel, tabService, layoutManager, inputHandler,
                textMeasurer, config, scrollManager, pointerEventHandler, themeManager);

            var clipboardManager = Services.GetRequiredService<IClipboardManager>();
            if (clipboardManager is ClipboardManager cm) cm.TopLevelRef = desktop.MainWindow;

            var fileService = Services.GetRequiredService<IFileDialogService>();
            if (fileService is IFileDialogService fs) fs.TopLevelRef = desktop.MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ICursorManager, CursorManager>();
        services.AddSingleton<IInputManager, InputManager>();
        services.AddSingleton<ITextMeasurer, AvaloniaTextMeasurer>();
        services.AddSingleton<IClipboardManager, ClipboardManager>();
        services.AddSingleton<ITextMeasurer, AvaloniaTextMeasurer>();
        services.AddSingleton<ISelectionManager, SelectionManager>();
        services.AddSingleton<ITextAnalysisService, TextAnalysisService>();
        services.AddSingleton<IScrollManager, ScrollManager>();
        services.AddSingleton<ITabService, TabService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IThemeManager>(sp => ThemeManager.Instance);

        // Editor Services
        services.AddSingleton<IEditorLayoutManager, EditorLayoutManager>();
        services.AddSingleton<IEditorInputHandler, EditorInputHandler>();
        services.AddSingleton<IPointerEventHandler, PointerEventHandler>();

        // Editor Commands
        services.AddSingleton<ISelectAllCommandHandler, SelectAllCommandHandler>();
        services.AddSingleton<IModifierKeyHandler, ModifierKeyHandler>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ITabViewModel, TabViewModel>();

        // Factories
        services.AddSingleton<ITabViewModelFactory, TabViewModelFactory>();
        services.AddSingleton<IEditorInstanceFactory, EditorInstanceFactory>();

        // Config
        services.AddSingleton<IEditorConfig, EditorConfig>();
    }
}