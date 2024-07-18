using System.Text;

namespace meteor.Core.Interfaces.Services;

public interface ITextBufferService
{
    int Length { get; }
    char this[int index] { get; }
    void Insert(int index, string text);
    void Delete(int index, int length);
    string Substring(int start, int length);
    void GetTextSegment(int start, int length, StringBuilder output);
    string GetText();
    void ReplaceAll(string newText);
    int LastIndexOf(char value, int startIndex = -1);
    int IndexOf(char value, int startIndex = 0);
    void Iterate(Action<char> action);
}