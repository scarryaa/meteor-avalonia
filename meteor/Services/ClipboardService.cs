using System.Threading.Tasks;
using Avalonia.Controls;
using meteor.Interfaces;

namespace meteor.Services;

public class ClipboardService : IClipboardService
{
    private readonly TopLevel _topLevel;

    public ClipboardService(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public async Task<string> GetTextAsync()
    {
        return await _topLevel.Clipboard.GetTextAsync();
    }

    public async Task SetTextAsync(string text)
    {
        await _topLevel.Clipboard.SetTextAsync(text);
    }
}