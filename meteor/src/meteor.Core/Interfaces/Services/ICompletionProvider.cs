using meteor.Core.Services;

namespace meteor.Core.Interfaces.Services;

public interface ICompletionProvider
{
    List<CompletionItem> GetCompletions(int cursorPosition);
}