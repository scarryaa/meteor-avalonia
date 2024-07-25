using Avalonia;
using Avalonia.Controls;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Services;

public class ClipboardManager : IClipboardManager
{
    public object TopLevelRef { get; set; }

    public async Task<string> GetTextAsync()
    {
        EnsureTopLevelRef();
        return await TopLevel.GetTopLevel((Visual)TopLevelRef).Clipboard.GetTextAsync();
    }

    public async Task SetTextAsync(string text)
    {
        EnsureTopLevelRef();
        await TopLevel.GetTopLevel((Visual)TopLevelRef).Clipboard.SetTextAsync(text);
    }

    public async Task CutAsync(string text)
    {
        await SetTextAsync(text);
    }

    public async Task CopyAsync(string text)
    {
        await SetTextAsync(text);
    }

    public async Task<string> PasteAsync()
    {
        return await GetTextAsync();
    }

    private void EnsureTopLevelRef()
    {
        if (TopLevelRef == null)
            throw new InvalidOperationException(
                "No top-level window reference found. Unable to perform clipboard operations.");
    }
}