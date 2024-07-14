namespace meteor.core.Models;

public class TextLine
{
    public int LineNumber { get; }
    public int Start { get; }
    public int Length { get; }
    public bool IsWrapped { get; }

    public TextLine(int lineNumber, int start, int length, bool isWrapped = false)
    {
        LineNumber = lineNumber;
        Start = start;
        Length = length;
        IsWrapped = isWrapped;
    }

    public int End => Start + Length;

    public override string ToString()
    {
        return $"Line {LineNumber}: Start={Start}, Length={Length}, IsWrapped={IsWrapped}";
    }
}