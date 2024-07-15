using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Utils;

public class TextEditorUtils
{
    public (long start, long end) FindWordOrSymbolBoundaries(ITextEditorViewModel viewModel, int position)
    {
        var text = viewModel.TextBuffer.Text;
        var start = position;
        var end = position;

        while (start > 0 && IsWordOrSymbolChar(text[start - 1])) start--;
        while (end < text.Length && IsWordOrSymbolChar(text[end])) end++;

        return (start, end);
    }

    private bool IsWordOrSymbolChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || char.IsSymbol(c);
    }
}