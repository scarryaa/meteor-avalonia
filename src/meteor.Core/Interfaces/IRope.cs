namespace meteor.Core.Interfaces;

public interface IRope
{
    int Length { get; }
    char this[int index] { get; }
    IRope Insert(int index, string s);
    IRope Delete(int index, int length);
    IRope GetSubstring(int start, int length);
    string Substring(int startIndex, int length);
    string ToString();
    IRope Concat(IRope other);
    void Iterate(Action<char> action);
}