using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using meteor.Core.Interfaces;

namespace meteor.App.Services;

public class AvaloniaClipboardService : IClipboardService
{
    private readonly Visual _visual;
    private IClipboard? _clipboard;

    public AvaloniaClipboardService(Visual visual)
    {
        _visual = visual;
    }

    private IClipboard Clipboard => _clipboard ??= GetClipboard();

    private IClipboard GetClipboard()
    {
        var clipboard = TopLevel.GetTopLevel(_visual)?.Clipboard;
        return clipboard ?? throw new InvalidOperationException("Clipboard is not available.");
    }

    public async Task<string> GetTextAsync()
    {
        return await Clipboard.GetTextAsync() ?? string.Empty;
    }

    public Task SetTextAsync(string text)
    {
        return Clipboard.SetTextAsync(text);
    }
}