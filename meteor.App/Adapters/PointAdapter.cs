using Avalonia;
using meteor.Core.Interfaces.Rendering;

namespace meteor.App.Adapters;

public class PointAdapter : IPoint
{
    private readonly Point _point;

    public PointAdapter(Point point)
    {
        _point = point;
    }

    public double X => _point.X;
    public double Y => _point.Y;
}