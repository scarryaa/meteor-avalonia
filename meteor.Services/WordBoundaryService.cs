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

        // Ensure position is within bounds
        position = Math.Min(position, text.Length);

        // If we're already at a word boundary, move back one position
        if (position < text.Length && IsWordBoundary(text[position])) position--;

        // Find the previous word boundary
        while (position > 0 && !IsWordBoundary(text[position - 1])) position--;

        return position;
    }

    public int GetNextWordBoundary(ITextBuffer textBuffer, int position)
    {
        var text = textBuffer.Text;
        var length = text.Length;

        // If we're at or beyond the end of the text, return the current position
        if (position >= length)
            return position;

        // If we're already at the end of a word, return the current position
        if (position > 0 && IsWordCharacter(text[position - 1]) && !IsWordCharacter(text[position]))
            return position;

        // Move to the end of the current word
        while (position < length && IsWordCharacter(text[position]))
            position++;

        // If we're at the start of the text, move to the first non-space character
        if (position == 0)
        {
            while (position < length && char.IsWhiteSpace(text[position]))
                position++;
            return position;
        }

        // If we're now at a non-word character, this is the end of the current word
        return position;
    }

    private bool IsWordCharacter(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '-';
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

    private bool IsWordBoundary(char c)
    {
        return WordBoundaryRegex.IsMatch(c.ToString());
    }
}