using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class Rect : IRect
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public Rect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}