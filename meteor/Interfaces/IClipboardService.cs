using System.Threading.Tasks;

namespace meteor.Interfaces;

public interface IClipboardService
{
    Task<string> GetTextAsync();
    Task SetTextAsync(string text);
}