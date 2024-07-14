namespace meteor.core.Interfaces;

public interface IClipboard
{
    Task SetTextAsync(string text);
    Task<string> GetTextAsync();
}