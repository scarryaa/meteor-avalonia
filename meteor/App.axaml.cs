using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using meteor.Interfaces;
using meteor.Services;
using meteor.ViewModels;
using meteor.Views;
using Microsoft.Extensions.DependencyInjection;

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
    }
}