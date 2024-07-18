using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using meteor.Application.Interfaces;
using meteor.Application.Services;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Infrastructure.Data;
using meteor.UI.ViewModels;
using meteor.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.UI;

public class App : Avalonia.Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
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
        services.AddSingleton<IRope, Rope>(sp => new Rope(""));
        services.AddSingleton<ITextBufferService, TextBufferService>();
        services.AddSingleton<ISyntaxHighlighter, SyntaxHighlighter>();
        services.AddSingleton<ICursorService, CursorService>();
        services.AddSingleton<ISelectionService, SelectionService>();
        services.AddSingleton<ITextAnalysisService, TextAnalysisService>();
        services.AddSingleton<IInputService, InputService>();

        services.AddTransient<IEditorViewModel, EditorViewModel>();

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
}