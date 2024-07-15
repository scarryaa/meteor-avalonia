using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class Pen : IPen
{
    public IBrush Brush { get; }
    public double Thickness { get; }
    public double[] DashArray { get; }
    public double DashOffset { get; }

    public Pen(IBrush brush, double thickness, double[] dashArray = null, double dashOffset = 0)
    {
        Brush = brush;
        Thickness = thickness;
        DashArray = dashArray;
        DashOffset = dashOffset;
    }
}