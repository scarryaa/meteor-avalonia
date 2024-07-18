namespace meteor.Core.Interfaces;

public interface ITextBuffer : IDisposable
{
    int Length { get; }
    char this[int index] { get; }
    void Insert(int index, string text);
    void Delete(int index, int length);
    string Substring(int start, int length);
    string GetText();
    string GetText(int start, int length);
    void ReplaceAll(string newText);
    void Iterate(Action<char> action);
}