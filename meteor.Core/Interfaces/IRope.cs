namespace meteor.Core.Interfaces;

public interface IRope
{
    int Length { get; set; }
    int LineCount { get; }
    void Insert(int index, string text);
    void Delete(int start, int length);
    string GetText();
    string GetText(int start, int length);
    string GetLineText(int lineIndex);
    int GetLineIndexFromPosition(int position);
    int GetLineStartPosition(int lineIndex);
    int GetLineEndPosition(int lineIndex);
    int GetLineLength(int lineIndex);
    int IndexOf(char value, int startIndex = 0);
}