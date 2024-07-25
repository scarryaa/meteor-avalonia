namespace meteor.Core.Models;

public class Selection
{
    public int Start { get; set; }
    public int End { get; set; }

    public Selection(int start, int end)
    {
        Start = start;
        End = end;
    }
}