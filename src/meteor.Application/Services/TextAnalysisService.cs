using meteor.Core.Interfaces.Services;

namespace meteor.Application.Services;

public class TextAnalysisService : ITextAnalysisService
{
    public (int start, int end) GetWordBoundariesAt(string text, int index)
    {
        if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
            return (index, index);

        var start = index;
        var end = index;

        // If we're on a non-letter/digit, return that position
        if (!char.IsLetterOrDigit(text[index]))
            return (index, index);

        // Find the start of the word
        while (start > 0 && char.IsLetterOrDigit(text[start - 1]))
            start--;

        // Find the end of the word
        while (end < text.Length && char.IsLetterOrDigit(text[end]))
            end++;

        return (start, end);
    }

    public (int start, int end) GetLineBoundariesAt(string text, int index)
    {
        var start = index;
        var end = index;

        while (start > 0 && text[start - 1] != '\n') start--;

        while (end < text.Length && text[end] != '\n') end++;

        return (start, end);
    }

    public int GetPositionAbove(string text, int index)
    {
        if (string.IsNullOrEmpty(text) || index <= 0)
            return 0;

        var previousNewLine = text.LastIndexOf('\n', Math.Max(0, index - 1));
        if (previousNewLine == -1)
            return 0;

        var startOfCurrentLine = text.LastIndexOf('\n', previousNewLine - 1);
        var columnOnCurrentLine = index - (previousNewLine + 1);

        return Math.Min(startOfCurrentLine + 1 + columnOnCurrentLine, previousNewLine);
    }

    public int GetPositionBelow(string text, int index)
    {
        var currentLineStart = text.LastIndexOf('\n', Math.Max(0, index - 1)) + 1;
        var nextLineStart = text.IndexOf('\n', index);
        var column = index - currentLineStart;

        if (nextLineStart == -1)
            // We're on the last line, move to the end of the text
            return text.Length;

        nextLineStart++; // Move past the newline character
        return Math.Min(nextLineStart + column, text.Length);
    }
}