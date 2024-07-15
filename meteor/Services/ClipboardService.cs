using System.Threading.Tasks;
using Avalonia.Controls;
using meteor.Interfaces;

namespace meteor.Services;

public class ClipboardService(TopLevel topLevel) : IClipboardService
{
    public async Task<string> GetTextAsync()
    {
        return await topLevel.Clipboard.GetTextAsync();
    }

    public async Task SetTextAsync(string text)
    {
        await topLevel.Clipboard.SetTextAsync(text);
    }
}