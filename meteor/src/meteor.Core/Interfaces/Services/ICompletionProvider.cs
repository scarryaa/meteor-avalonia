using meteor.Core.Services;

namespace meteor.Core.Interfaces.Services;

public interface ICompletionProvider
{
    Task<IEnumerable<CompletionItem>> GetCompletionsAsync(int cursorPosition,
        CancellationToken cancellationToken = default);
}