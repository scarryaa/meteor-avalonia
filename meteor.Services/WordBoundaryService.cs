using System.Text.RegularExpressions;
using meteor.Core.Interfaces;

namespace meteor.Services;

public class WordBoundaryService : IWordBoundaryService
{
    private static readonly Regex WordBoundaryRegex = new(@"\b", RegexOptions.Compiled);

    public (int start, int end) GetWordBoundaries(ITextBuffer textBuffer, int position)
    {
        var text = textBuffer.Text;
        if (position < 0 || position >= text.Length) return (position, position);

        var start = position;
        var end = position;

        // Find start of the word
        while (start > 0 && !WordBoundaryRegex.IsMatch(text[start - 1].ToString())) start--;

        // Find end of the word
        while (end < text.Length && !WordBoundaryRegex.IsMatch(text[end].ToString())) end++;

        return (start, end);
    }

    public int GetPreviousWordBoundary(ITextBuffer textBuffer, int position)
    {
        var text = textBuffer.Text;
        if (position <= 0) return 0;

        var newPosition = position - 1;
        while (newPosition > 0 && !WordBoundaryRegex.IsMatch(text[newPosition].ToString())) newPosition--;

        return newPosition;
    }

    public int GetNextWordBoundary(ITextBuffer textBuffer, int position)
    {
        var text = textBuffer.Text;
        if (position >= text.Length) return text.Length;

        var newPosition = position + 1;
        while (newPosition < text.Length && !WordBoundaryRegex.IsMatch(text[newPosition].ToString())) newPosition++;

        return newPosition;
    }
}