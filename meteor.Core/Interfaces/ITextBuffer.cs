using meteor.Core.Models.Events;

namespace meteor.Core.Interfaces;

public interface ITextBuffer
{
    string Text { get; }
    int Length { get; }
    int LineCount { get; }

    event EventHandler<TextChangedEventArgs> TextChanged;

    string GetText(int start, int length);
    void InsertText(int position, string text);
    void DeleteText(int start, int length);
    void SetText(string newText);
    void Clear();
    string? GetLineText(int lineIndex);
    int GetLineStartPosition(int lineIndex);
    int GetLineEndPosition(int lineIndex);
    int GetLineLength(int lineIndex);
    int GetLineIndexFromPosition(int position);
    List<int> GetLineStarts();
}