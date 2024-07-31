namespace meteor.Core.Models;

public struct CursorPosition
{
    public int Line { get; set; }
    public int Column { get; set; }

    public CursorPosition(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return $"{Line}, {Column}";
    }
}