namespace meteor.Core.Interfaces.Services;

public interface ITextBufferService
{
    string GetContent();
    string GetContentSlice(int startLine, int endLine);
    void InsertText(int position, string text);
    void DeleteText(int position, int length);
    int GetLength();
    int GetLineCount();
    double GetMaxLineWidth(string fontFamily, double fontSize);
    string GetEntireContent();
}