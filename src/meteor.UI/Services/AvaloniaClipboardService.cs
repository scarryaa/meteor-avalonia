using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Services;

public class AvaloniaClipboardService : IClipboardService
{
    private readonly object? _visualReference;

    public AvaloniaClipboardService(object? visualReference)
    {
        _visualReference = visualReference;
    }

    async Task<string> IClipboardService.GetText()
    {
        if (_visualReference is not null)
            return await TopLevel.GetTopLevel((Visual)_visualReference).Clipboard.GetTextAsync();
        throw new InvalidOperationException("Visual reference is not set.");
    }

    async Task IClipboardService.SetText(string text)
    {
        if (_visualReference is not null)
            await TopLevel.GetTopLevel((Visual)_visualReference).Clipboard.SetTextAsync(text);
        else throw new InvalidOperationException("Visual reference is not set.");
    }
}