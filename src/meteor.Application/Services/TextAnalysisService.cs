using meteor.Core.Interfaces.Services;

namespace meteor.Application.Services;

public class TextAnalysisService : ITextAnalysisService
{
    public (int start, int end) GetWordBoundaries(ITextBufferService textBuffer, int index)
    {
        if (textBuffer.Length == 0 || index < 0 || index >= textBuffer.Length)
            return (Math.Min(index, textBuffer.Length), Math.Min(index, textBuffer.Length));

        var start = index;
        var end = index;

        // Helper function to check if a character is part of a word
        bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        // If we're on a space, move to the next word
        while (start < textBuffer.Length && char.IsWhiteSpace(textBuffer[start]))
            start++;

        // If we're at the end of the buffer, return
        if (start >= textBuffer.Length)
            return (start, start);

        // Find the start of the word
        while (start > 0 && IsWordChar(textBuffer[start - 1]))
            start--;

        // Find the end of the word
        end = start;
        while (end < textBuffer.Length && IsWordChar(textBuffer[end]))
            end++;

        return (start, end);
    }

    public (int start, int end) GetLineBoundaries(ITextBufferService textBuffer, int index)
    {
        var start = GetLineStart(textBuffer, index);
        var end = GetLineEnd(textBuffer, index);
        return (start, end);
    }

    public int GetPositionAbove(ITextBufferService textBuffer, int index)
    {
        if (textBuffer.Length == 0 || index <= 0)
            return 0;

        var currentLineStart = GetLineStart(textBuffer, index);
        if (currentLineStart == 0)
            return 0;

        var previousLineEnd = textBuffer.LastIndexOf('\n', currentLineStart - 2);
        var previousLineStart = previousLineEnd == -1 ? 0 : previousLineEnd + 1;
        var columnOnCurrentLine = index - currentLineStart;

        return Math.Min(previousLineStart + columnOnCurrentLine, currentLineStart - 1);
    }

    public int GetPositionBelow(ITextBufferService textBuffer, int index)
    {
        var currentLineStart = GetLineStart(textBuffer, index);
        var nextLineStart = textBuffer.IndexOf('\n', index);
        if (nextLineStart == -1)
            return textBuffer.Length;

        nextLineStart++; // Move past the newline character
        var columnOnCurrentLine = index - currentLineStart;

        return Math.Min(nextLineStart + columnOnCurrentLine, textBuffer.Length);
    }

    public int GetWordStart(ITextBufferService textBuffer, int index)
    {
        if (index <= 0 || index >= textBuffer.Length)
            return index;

        // Move back to the start of the current word or whitespace
        while (index > 0 && !char.IsWhiteSpace(textBuffer[index - 1]))
            index--;

        // Move forward past any whitespace
        while (index < textBuffer.Length && char.IsWhiteSpace(textBuffer[index]))
            index++;

        return index;
    }

    public int GetWordEnd(ITextBufferService textBuffer, int index)
    {
        if (index < 0 || index >= textBuffer.Length)
            return index;

        while (index < textBuffer.Length && !char.IsLetterOrDigit(textBuffer[index]))
            index++;

        while (index < textBuffer.Length && char.IsLetterOrDigit(textBuffer[index]))
            index++;

        return index;
    }

    public int GetLineStart(ITextBufferService textBuffer, int index)
    {
        while (index > 0 && textBuffer[index - 1] != '\n')
            index--;
        return index;
    }

    public int GetLineEnd(ITextBufferService textBuffer, int index)
    {
        while (index < textBuffer.Length && textBuffer[index] != '\n')
            index++;
        return index;
    }
}