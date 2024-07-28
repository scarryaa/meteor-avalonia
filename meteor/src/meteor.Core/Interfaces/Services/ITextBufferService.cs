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
    int GetLineStartOffset(int lineIndex);
    int GetLineEndOffset(int lineIndex);
    int GetDocumentVersion();

    void LoadContent(string content);
    int GetLineIndexFromCharacterIndex(int charIndex);
    int GetCharacterIndexFromLineIndex(int lineIndex);
    void Replace(int start, int length, string newText);
}