using Avalonia.Controls;
using meteor.core.Interfaces;

namespace meteor.rendering.Services;

public class AvaloniaClipboard : IClipboard
{
    private readonly TopLevel _topLevel;

    public AvaloniaClipboard(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public Task SetTextAsync(string text)
    {
        return _topLevel.Clipboard.SetTextAsync(text);
    }

    public Task<string> GetTextAsync()
    {
        return _topLevel.Clipboard.GetTextAsync();
    }
}