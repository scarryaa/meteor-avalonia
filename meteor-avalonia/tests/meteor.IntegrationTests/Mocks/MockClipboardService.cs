using meteor.Core.Interfaces.Services;

namespace meteor.IntegrationTests.Mocks;

public class MockClipboardService : IClipboardService
{
    private string _clipboardText = string.Empty;

    public Task<string> GetText()
    {
        return Task.FromResult(_clipboardText);
    }

    public Task SetText(string text)
    {
        _clipboardText = text;
        return Task.CompletedTask;
    }
}