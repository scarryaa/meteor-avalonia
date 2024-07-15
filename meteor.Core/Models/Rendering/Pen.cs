using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class Pen(IBrush brush, double thickness, double[] dashArray = null, double dashOffset = 0)
    : IPen
{
    public IBrush Brush { get; } = brush;
    public double Thickness { get; } = thickness;
    public double[] DashArray { get; } = dashArray;
    public double DashOffset { get; } = dashOffset;
}