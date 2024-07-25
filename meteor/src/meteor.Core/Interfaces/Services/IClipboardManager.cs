namespace meteor.Core.Interfaces.Services;

public interface IClipboardManager
{
    object TopLevelRef { get; set; }
    Task<string> GetTextAsync();
    Task SetTextAsync(string text);
    Task CutAsync(string text);
    Task CopyAsync(string text);
    Task<string> PasteAsync();
}