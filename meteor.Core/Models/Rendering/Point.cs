using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class Point(double x, double y) : IPoint
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
}