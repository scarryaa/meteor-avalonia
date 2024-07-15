using System;
using System.Windows.Input;
using meteor.Interfaces;
using meteor.Models;
using meteor.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.Services;

public class TabFactory(IServiceProvider serviceProvider) : ITabFactory
{
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
        var mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

        return (
            CursorPositionService: serviceProvider.GetRequiredService<ICursorPositionService>(),
            UndoRedoManager: serviceProvider.GetRequiredService<IUndoRedoManager<TextState>>(),
            FileSystemWatcherFactory: serviceProvider.GetRequiredService<IFileSystemWatcherFactory>(),
            TextBufferFactory: serviceProvider.GetRequiredService<ITextBufferFactory>(),
            FontPropertiesViewModel: serviceProvider.GetRequiredService<FontPropertiesViewModel>(),
            LineCountViewModel: serviceProvider.GetRequiredService<LineCountViewModel>(),
            ClipboardService: serviceProvider.GetRequiredService<IClipboardService>(),
            AutoSaveService: serviceProvider.GetRequiredService<IAutoSaveService>(),
            ThemeService: serviceProvider.GetRequiredService<IThemeService>(),
            mainWindowViewModel.CloseTabCommand,
            mainWindowViewModel.CloseOtherTabsCommand,
            mainWindowViewModel.CloseAllTabsCommand
        );
    }
}