using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using meteor.Core.Config;
using meteor.Core.Interfaces;
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

    [RequiresUnreferencedCode("Application startup code may require reflection.")]
    public override void OnFrameworkInitializationCompleted()
    {
        LoadNativeLibrary();
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
            var fileService = Services.GetRequiredService<IFileService>();
            var gitService = Services.GetRequiredService<IGitService>();
            var searchService = Services.GetRequiredService<ISearchService>();
            var settingsService = Services.GetRequiredService<ISettingsService>();
            themeManager.Initialize(Services.GetRequiredService<ISettingsService>());

            IsActiveToBrushConverter.Initialize(themeManager);

            desktop.MainWindow = new MainWindow(mainWindowViewModel, tabService, layoutManager, inputHandler,
                textMeasurer, config, scrollManager, pointerEventHandler, themeManager, fileService, gitService, searchService, settingsService);

            var clipboardManager = Services.GetRequiredService<IClipboardManager>();
            if (clipboardManager is ClipboardManager cm) cm.TopLevelRef = desktop.MainWindow;

            var fileDialogService = Services.GetRequiredService<IFileDialogService>();
            if (fileDialogService is FileDialogService fds) fds.TopLevelRef = desktop.MainWindow;

            base.OnFrameworkInitializationCompleted();
        }
    }

    private void LoadNativeLibrary()
    {
        var libraryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "meteor_rust_core.dll"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? "libmeteor_rust_core.dylib"
                : "libmeteor_rust_core.so";

        var libraryPath = Path.Combine(AppContext.BaseDirectory, libraryName);
        if (!File.Exists(libraryPath) && !File.Exists("./" + libraryName))
            throw new FileNotFoundException($"Native library not found: {libraryPath}");

        NativeLibrary.Load(libraryPath);
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
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGitService, GitService>(sp => new GitService(""));
        services.AddSingleton<ISearchService, SearchService>();

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