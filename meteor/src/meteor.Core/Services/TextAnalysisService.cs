using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class TextAnalysisService : ITextAnalysisService
{
    private int _desiredColumn;
    private static readonly char[] CodeSymbols = { '.', ',', ';', ':', '(', ')', '[', ']', '{', '}', '<', '>', '=', '+', '-', '*', '/', '\\', '|', '&', '^', '%', '$', '#', '@', '!', '?', '\'', '"' };

    public int FindPreviousWordBoundary(string text, int currentPosition)
    {
        if (string.IsNullOrEmpty(text) || currentPosition <= 0) return 0;

        var position = Math.Min(currentPosition, text.Length) - 1;
        var lineStart = FindStartOfCurrentLine(text, position);

        // Handle empty lines
        while (position > lineStart && text[position] == '\n' && text[position - 1] == '\n') position--;

        position = SkipWhitespace(text, position, -1, lineStart);
        while (position > lineStart && !char.IsWhiteSpace(text[position - 1]) && !CodeSymbols.Contains(text[position - 1])) position--;

        return position;
    }

    public int FindNextWordBoundary(string text, int currentPosition)
    {
        if (string.IsNullOrEmpty(text) || currentPosition >= text.Length) return text.Length;

        var position = currentPosition;
        var lineEnd = FindEndOfCurrentLine(text, position);

        // Handle empty lines
        while (position < lineEnd - 1 && text[position] == '\n' && text[position + 1] == '\n') position++;

        position = SkipWhitespace(text, position, 1, lineEnd);
        while (position < lineEnd && !char.IsWhiteSpace(text[position]) && !CodeSymbols.Contains(text[position])) position++;

        return position;
    }

    public int FindStartOfCurrentLine(string text, int currentPosition)
    {
        if (string.IsNullOrEmpty(text) || currentPosition <= 0) return 0;
        return text.LastIndexOf('\n', Math.Min(currentPosition - 1, text.Length - 1)) + 1;
    }

    public int FindEndOfCurrentLine(string text, int currentPosition)
    {
        if (string.IsNullOrEmpty(text) || currentPosition >= text.Length) return text.Length;
        var nextNewLine = text.IndexOf('\n', currentPosition);
        return nextNewLine == -1 ? text.Length : nextNewLine;
    }

    public int FindEndOfPreviousLine(string text, int currentPosition)
    {
        if (string.IsNullOrEmpty(text) || currentPosition <= 0) return 0;
        var prevNewLine = text.LastIndexOf('\n', Math.Max(0, currentPosition - 2));
        return prevNewLine == -1 ? 0 : prevNewLine;
    }

    public int FindEquivalentPositionInLine(string text, int currentPosition, int lineStart, int lineEnd)
    {
        var currentLineStart = FindStartOfCurrentLine(text, currentPosition);
        var currentOffset = currentPosition - currentLineStart;

        return lineStart == lineEnd ? lineStart : Math.Min(lineStart + currentOffset, lineEnd);
    }

    public int GetLineNumber(string text, int position) => GetLineFromPosition(text, position);

    public int GetLineFromPosition(string text, int position)
    {
        return string.IsNullOrEmpty(text) || position < 0 ? 0 : text.Take(position).Count(c => c == '\n');
    }

    public void SetDesiredColumn(int column) => _desiredColumn = column;

    public int GetDesiredColumn() => _desiredColumn;

    public int GetLineCount(string text) => string.IsNullOrEmpty(text) ? 0 : text.Count(c => c == '\n') + 1;

    public int GetPositionFromLine(string text, int lineNumber)
    {
        if (string.IsNullOrEmpty(text) || lineNumber < 0) return 0;
        return text.Split('\n').Take(lineNumber).Sum(line => line.Length + 1);
    }

    public int GetEndOfLine(string text, int lineNumber)
    {
        var startOfLine = GetPositionFromLine(text, lineNumber);
        return FindEndOfCurrentLine(text, startOfLine);
    }

    public void ResetDesiredColumn() => _desiredColumn = 0;

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
        if (currentLineEnd >= text.Length) return text.Length;

        var nextLineStart = currentLineEnd + 1;
        var nextLineEnd = FindEndOfCurrentLine(text, nextLineStart);
        return CalculatePositionInLine(text, nextLineStart, nextLineEnd, _desiredColumn);
    }

    private static int SkipWhitespace(string text, int start, int direction, int boundary)
    {
        var position = start;
        while ((direction > 0 && position < boundary) || (direction < 0 && position > boundary))
        {
            if (!char.IsWhiteSpace(text[position]) || text[position] == '\n' || CodeSymbols.Contains(text[position]))
                break;
            position += direction;
        }
        return position;
    }

    private static int CalculatePositionInLine(string text, int lineStart, int lineEnd, int targetColumn)
    {
        var lineLength = lineEnd - lineStart;
        return lineStart + Math.Min(lineLength, targetColumn);
    }
}