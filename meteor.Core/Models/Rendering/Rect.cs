using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class Rect(double x, double y, double width, double height)
    : IRect
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Width { get; set; } = width;
    public double Height { get; set; } = height;
}