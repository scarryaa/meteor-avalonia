namespace meteor.Core.Interfaces.Services;

public interface IClipboardService
{
    Task<string> GetText();
    Task SetText(string text);
}