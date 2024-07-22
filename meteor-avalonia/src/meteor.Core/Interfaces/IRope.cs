namespace meteor.Core.Interfaces;

public interface IRope
{
    int Length { get; }
    char this[int index] { get; }

    void Insert(int index, string? s);
    void Delete(int index, int length);
    string Substring(int startIndex, int length);
    string ToString();
}