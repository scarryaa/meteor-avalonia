namespace meteor.Core.Interfaces.Services;

public interface ITextBufferService
{
    string GetContent();
    void InsertText(int position, string text);
    void DeleteText(int position, int length);
    int GetLength();
}