using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class TextAnalysisService : ITextAnalysisService
{
    private int _desiredColumn;

    public int FindPreviousWordBoundary(string text, int currentPosition)
    {
        if (string.IsNullOrEmpty(text) || currentPosition <= 0) return 0;

        var position = currentPosition - 1;

        // Skip trailing whitespace
        while (position > 0 && char.IsWhiteSpace(text[position])) position--;

        // Find the start of the current word
        while (position > 0 && !char.IsWhiteSpace(text[position - 1])) position--;

        return position;
    }

    public int GetLineFromPosition(string text, int position)
    {
        if (string.IsNullOrEmpty(text) || position < 0) return 0;

        var lineCount = 1;
        for (var i = 0; i < position; i++)
            if (text[i] == '\n')
                lineCount++;
        return lineCount - 1;
    }

    public void SetDesiredColumn(int column)
    {
        _desiredColumn = column;
    }

    public int GetDesiredColumn()
    {
        return _desiredColumn;
    }

    public int FindNextWordBoundary(string text, int currentPosition)
    {
        if (string.IsNullOrEmpty(text) || currentPosition >= text.Length) return text.Length;

        var position = currentPosition;

        // Skip leading whitespace
        while (position < text.Length && char.IsWhiteSpace(text[position])) position++;

        // Find the end of the current word
        while (position < text.Length && !char.IsWhiteSpace(text[position])) position++;

        return position;
    }

    public int FindStartOfCurrentLine(string text, int currentPosition)
    {
        if (string.IsNullOrEmpty(text) || currentPosition <= 0) return 0;

        var position = currentPosition;

        while (position > 0 && text[position - 1] != '\n') position--;

        return position;
    }

    public int FindEndOfCurrentLine(string text, int currentPosition)
    {
        if (string.IsNullOrEmpty(text) || currentPosition >= text.Length) return text.Length;

        var position = currentPosition;

        while (position < text.Length && text[position] != '\n') position++;

        return position;
    }

    public int FindEndOfPreviousLine(string text, int currentPosition)
    {
        if (string.IsNullOrEmpty(text) || currentPosition <= 0) return 0;

        var position = currentPosition - 1;

        // Move to the end of the previous line
        while (position > 0 && text[position] != '\n') position--;

        // If we're not at the start of the text, move back one more to get before the newline
        if (position > 0) position--;

        return position;
    }

    public int FindEquivalentPositionInLine(string text, int currentPosition, int lineStart, int lineEnd)
    {
        var currentLineStart = FindStartOfCurrentLine(text, currentPosition);
        var currentOffset = currentPosition - currentLineStart;
        var newLineLength = lineEnd - lineStart;

        return Math.Min(lineStart + currentOffset, lineEnd);
    }

    public int GetLineNumber(string text, int position)
    {
        if (string.IsNullOrEmpty(text) || position < 0) return 0;

        var lineCount = 0;
        var currentPosition = 0;

        while (currentPosition < position && currentPosition < text.Length)
        {
            if (text[currentPosition] == '\n') lineCount++;
            currentPosition++;
        }

        return lineCount;
    }

    public int GetLineCount(string text)
    {
        return string.IsNullOrEmpty(text) ? 0 : text.Count(c => c == '\n') + 1;
    }

    public int GetPositionFromLine(string text, int lineNumber)
    {
        if (string.IsNullOrEmpty(text) || lineNumber < 0) return 0;

        var currentLine = 0;
        var currentPosition = 0;

        while (currentLine < lineNumber && currentPosition < text.Length)
        {
            if (text[currentPosition] == '\n') currentLine++;
            currentPosition++;
        }

        return currentPosition;
    }

    public int GetEndOfLine(string text, int lineNumber)
    {
        var startOfLine = GetPositionFromLine(text, lineNumber);
        var endOfLine = text.IndexOf('\n', startOfLine);
        return endOfLine == -1 ? text.Length : endOfLine;
    }

    public void ResetDesiredColumn()
    {
        _desiredColumn = 0;
    }

    public int FindPositionInLineAbove(string text, int currentPosition)
    {
        var currentLineStart = FindStartOfCurrentLine(text, currentPosition);
        var previousLineEnd = currentLineStart > 0 ? currentLineStart - 1 : 0;
        var previousLineStart = FindStartOfCurrentLine(text, previousLineEnd);

        return CalculatePositionInLine(text, previousLineStart, previousLineEnd, _desiredColumn);
    }

    public int FindPositionInLineBelow(string text, int currentPosition)
    {
        var currentLineEnd = FindEndOfCurrentLine(text, currentPosition);
        if (currentLineEnd >= text.Length - 1) return currentPosition; // Already at the last line

        var nextLineStart = currentLineEnd + 1;
        var nextLineEnd = FindEndOfCurrentLine(text, nextLineStart);

        return CalculatePositionInLine(text, nextLineStart, nextLineEnd, _desiredColumn);
    }

    private int CalculatePositionInLine(string text, int lineStart, int lineEnd, int targetColumn)
    {
        var lineLength = lineEnd - lineStart;
        return lineStart + Math.Min(lineLength, targetColumn);
    }
}