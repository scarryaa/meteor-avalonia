using Avalonia;
using meteor.Core.Interfaces.Rendering;

namespace meteor.App.Adapters;

public class PointAdapter(Point point) : IPoint
{
    public double X => point.X;
    public double Y => point.Y;
}