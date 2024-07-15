using meteor.Core.Interfaces;

namespace meteor.Services;

public class ClipboardService : IClipboardService
{
    public Task<string> GetTextAsync()
    {
        // Implementation depends on the specific platform
        throw new NotImplementedException("Implement platform-specific clipboard access here");
    }

    public Task SetTextAsync(string text)
    {
        // Implementation depends on the specific platform
        throw new NotImplementedException("Implement platform-specific clipboard access here");
    }
}