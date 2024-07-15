namespace meteor.Core.Interfaces;

public interface IVector
{
    double X { get; }
    double Y { get; }

    IVector WithX(double x);
    IVector WithY(double y);
}