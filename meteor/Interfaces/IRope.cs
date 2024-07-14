namespace meteor.Interfaces;

public interface IRope
{
    int Length { get; }
    int LineCount { get; }
    int LongestLineLength { get; }
    int LongestLineIndex { get; }
    long GetChangeCounter();
    int GetLineIndexFromPosition(int position);
    int GetLineStartPosition(int lineIndex);
    int GetLineEndPosition(int lineIndex);
    int GetLineLength(int lineIndex);
    void Insert(int index, string text);
    void Delete(int start, int length);
    string GetText();
    string GetText(int start, int length);
    string GetLineText(int lineIndex);
    int IndexOf(char value, int startIndex = 0);
    bool IsLineSelected(int lineIndex, long selectionStart, long selectionEnd);
}