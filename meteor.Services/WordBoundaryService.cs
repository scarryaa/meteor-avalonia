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

        var start = FindWordStart(text, position);
        var end = FindWordEnd(text, position);

        return (start, end);
    }

    public int GetPreviousWordBoundary(ITextBuffer textBuffer, int position)
    {
        var text = textBuffer.Text;
        if (position <= 0) return 0;

        return FindPreviousWordBoundary(text, position - 1);
    }

    public int GetNextWordBoundary(ITextBuffer textBuffer, int position)
    {
        var text = textBuffer.Text;
        if (position >= text.Length) return text.Length;

        return FindNextWordBoundary(text, position + 1);
    }

    private int FindWordStart(string text, int position)
    {
        var start = position;
        while (start > 0 && !IsWordBoundary(text[start - 1])) start--;
        return start;
    }

    private int FindWordEnd(string text, int position)
    {
        var end = position;
        while (end < text.Length && !IsWordBoundary(text[end])) end++;
        return end;
    }

    private int FindPreviousWordBoundary(string text, int position)
    {
        var newPosition = position;
        while (newPosition > 0 && !IsWordBoundary(text[newPosition])) newPosition--;
        return newPosition;
    }

    private int FindNextWordBoundary(string text, int position)
    {
        var newPosition = position;
        while (newPosition < text.Length && !IsWordBoundary(text[newPosition])) newPosition++;
        return newPosition;
    }

    private bool IsWordBoundary(char c)
    {
        return WordBoundaryRegex.IsMatch(c.ToString());
    }
}