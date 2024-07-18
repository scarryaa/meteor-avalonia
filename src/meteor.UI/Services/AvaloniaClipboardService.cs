using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Services;

public class AvaloniaClipboardService : IClipboardService
{
    private readonly Lazy<Window> _mainWindow;

    public AvaloniaClipboardService(Func<Window> mainWindowFactory)
    {
        _mainWindow = new Lazy<Window>(mainWindowFactory);
    }

    public async Task<string> GetText()
    {
        return await _mainWindow.Value.Clipboard.GetTextAsync() ?? string.Empty;
    }

    public async Task SetText(string text)
    {
        await _mainWindow.Value.Clipboard.SetTextAsync(text);
    }
}