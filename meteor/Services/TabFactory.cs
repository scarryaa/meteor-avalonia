using System;
using System.Windows.Input;
using meteor.Interfaces;
using meteor.Models;
using meteor.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.Services;

public class TabFactory : ITabFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TabFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TabViewModel CreateTab()
    {
        var services = GetTabViewModelServices();

        return new TabViewModel(
            services.CursorPositionService,
            services.UndoRedoManager,
            services.FileSystemWatcherFactory,
            services.TextBufferFactory,
            services.FontPropertiesViewModel,
            services.LineCountViewModel,
            services.ClipboardService,
            services.AutoSaveService,
            services.ThemeService,
            services.CloseTabCommand,
            services.CloseOtherTabsCommand,
            services.CloseAllTabsCommand)
        {
            Title = $"Untitled {Guid.NewGuid().ToString().Substring(0, 8)}",
            IsNew = true
        };
    }

    private (
        ICursorPositionService CursorPositionService,
        IUndoRedoManager<TextState> UndoRedoManager,
        IFileSystemWatcherFactory FileSystemWatcherFactory,
        ITextBufferFactory TextBufferFactory,
        FontPropertiesViewModel FontPropertiesViewModel,
        LineCountViewModel LineCountViewModel,
        IClipboardService ClipboardService,
        IAutoSaveService AutoSaveService,
        IThemeService ThemeService,
        ICommand CloseTabCommand,
        ICommand CloseOtherTabsCommand,
        ICommand CloseAllTabsCommand
        ) GetTabViewModelServices()
    {
        var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

        return (
            CursorPositionService: _serviceProvider.GetRequiredService<ICursorPositionService>(),
            UndoRedoManager: _serviceProvider.GetRequiredService<IUndoRedoManager<TextState>>(),
            FileSystemWatcherFactory: _serviceProvider.GetRequiredService<IFileSystemWatcherFactory>(),
            TextBufferFactory: _serviceProvider.GetRequiredService<ITextBufferFactory>(),
            FontPropertiesViewModel: _serviceProvider.GetRequiredService<FontPropertiesViewModel>(),
            LineCountViewModel: _serviceProvider.GetRequiredService<LineCountViewModel>(),
            ClipboardService: _serviceProvider.GetRequiredService<IClipboardService>(),
            AutoSaveService: _serviceProvider.GetRequiredService<IAutoSaveService>(),
            ThemeService: _serviceProvider.GetRequiredService<IThemeService>(),
            mainWindowViewModel.CloseTabCommand,
            mainWindowViewModel.CloseOtherTabsCommand,
            mainWindowViewModel.CloseAllTabsCommand
        );
    }
}