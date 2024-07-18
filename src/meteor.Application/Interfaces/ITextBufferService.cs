namespace meteor.Application.Interfaces;

public interface ITextBufferService
{
    int Length { get; }
    char this[int index] { get; }
    void Insert(int index, string text);
    void Delete(int index, int length);
    string Substring(int start, int length);
    string GetText();
    void ReplaceAll(string newText);
    void Iterate(Action<char> action);
}