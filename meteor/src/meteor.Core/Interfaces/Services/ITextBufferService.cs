namespace meteor.Core.Interfaces.Services;

public interface ITextBufferService
{
    string GetContent();
    string GetContentSlice(int startLine, int endLine);
    string GetContentSliceByIndex(int startIndex, int length);
    void InsertText(int position, string text);
    void DeleteText(int position, int length);
    int GetLength();
    int GetLineCount();
    double GetMaxLineWidth(string fontFamily, double fontSize);
    string GetEntireContent();

    int GetLineIndexFromCharacterIndex(int charIndex);
    int GetCharacterIndexFromLineIndex(int lineIndex);
}