using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Enums;
using meteor.Interfaces;
using meteor.ViewModels;
using meteor.Windows;
using Microsoft.Extensions.Logging;

namespace meteor.Services;

public class DialogService : IDialogService
{
    private readonly IMainWindowProvider _mainWindowProvider;
    private readonly ILogger<DialogService> _logger;
    
    public DialogService(IMainWindowProvider mainWindowProvider, IErrorLoggingService errorLoggingService)
    {
        _logger = ServiceLocator.GetService<ILogger<DialogService>>();
        _mainWindowProvider = mainWindowProvider;
    }

    public async Task<ContentDialogResult> ShowContentDialogAsync(
        MainWindowViewModel mainWindowViewModel,
        string title,
        string content,
        string primaryButtonText,
        string secondaryButtonText = null,
        string closeButtonText = null)
    {
        SetDialogOpenState(mainWindowViewModel, true);

        var viewModel = new ContentDialogViewModel
        {
            Title = title,
            Content = new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            CloseButtonText = closeButtonText
        };

        var dialog = new ContentDialog
        {
            DataContext = viewModel
        };

        var mainWindow = _mainWindowProvider.GetMainWindow();
        if (mainWindow != null)
        {
            var result = await dialog.ShowDialog(mainWindow);
            SetDialogOpenState(mainWindowViewModel, false);
            return result;
        }

        var fallbackResult = ShowConsoleFallbackDialog(title, content);
        SetDialogOpenState(mainWindowViewModel, false);
        return fallbackResult;
    }

    public async Task ShowErrorDialogAsync(MainWindowViewModel mainWindowViewModel, string message)
    {
        SetDialogOpenState(mainWindowViewModel, true);

        var mainWindow = _mainWindowProvider.GetMainWindow();
        if (mainWindow != null)
        {
            await ShowContentDialogAsync((MainWindowViewModel)mainWindow.DataContext, "Error", message, "OK");
            SetDialogOpenState(mainWindowViewModel, false);
        }
        else
        {
            SetDialogOpenState(mainWindowViewModel, false);
            _logger.LogError("Error dialog fallback");
            Console.WriteLine($"Error: {message}");
        }
    }

    protected virtual void SetDialogOpenState(MainWindowViewModel mainWindowViewModel, bool isOpen)
    {
        mainWindowViewModel.IsDialogOpen = isOpen;
    }

    protected virtual ContentDialogResult ShowConsoleFallbackDialog(string title, string content)
    {
        Console.WriteLine($"{title}\n{content}");
        Console.WriteLine("Press P for Primary, S for Secondary, or any other key to close.");
        var key = Console.ReadKey(true).Key;
        return key switch
        {
            ConsoleKey.P => ContentDialogResult.Primary,
            ConsoleKey.S => ContentDialogResult.Secondary,
            _ => ContentDialogResult.None
        };
    }
}