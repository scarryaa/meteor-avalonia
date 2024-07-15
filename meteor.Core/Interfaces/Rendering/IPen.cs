namespace meteor.Core.Interfaces.Rendering;

public interface IPen
{
    IBrush Brush { get; }
    double Thickness { get; }
    double[] DashArray { get; }
    double DashOffset { get; }
}