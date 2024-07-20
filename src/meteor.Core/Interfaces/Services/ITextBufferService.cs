using System.Text;

namespace meteor.Core.Interfaces.Services;

public interface ITextBufferService
{
    int Length { get; }
    char this[int index] { get; }
    void Insert(int index, string text);
    void Delete(int index, int length);
    void GetTextSegment(int start, int length, StringBuilder output);
    void GetTextSegment(int start, int length, char[] output);
    void ReplaceAll(string newText);
    int LastIndexOf(char value, int startIndex = -1);
    int IndexOf(char value, int startIndex = 0);
    void Iterate(Action<char> action);
    void AppendTo(StringBuilder sb);
    public string Substring(int start, int length);
    ReadOnlySpan<char> AsSpan(int start, int length);
}