namespace meteor.Core.Interfaces.Services;

public interface ITextAnalysisService
{
    int FindPreviousWordBoundary(string text, int currentPosition);
    int FindNextWordBoundary(string text, int currentPosition);
    int FindStartOfCurrentLine(string text, int currentPosition);
    int FindEndOfCurrentLine(string text, int currentPosition);
    int FindEndOfPreviousLine(string text, int currentPosition);
    int FindEquivalentPositionInLine(string text, int currentPosition, int lineStart, int lineEnd);
    int FindPositionInLineAbove(string text, int currentPosition);
    int FindPositionInLineBelow(string text, int currentPosition);
    void ResetDesiredColumn();
    void SetDesiredColumn(int desiredColumn);
    int GetDesiredColumn();
    int GetPositionFromLine(string text, int lineNumber);
    int GetLineNumber(string text, int position);
    int GetLineCount(string text);
    int GetEndOfLine(string text, int lineNumber);
    int GetLineFromPosition(string text, int position);
}