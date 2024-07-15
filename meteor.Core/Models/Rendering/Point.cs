using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class Point : IPoint
{
    public double X { get; set; }
    public double Y { get; set; }

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }
}