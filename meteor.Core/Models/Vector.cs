using meteor.Core.Interfaces;

namespace meteor.Core.Models;

public struct Vector : IVector
{
    public double X { get; }
    public double Y { get; }

    public Vector(double x, double y)
    {
        X = x;
        Y = y;
    }

    public IVector WithX(double x)
    {
        return new Vector(x, Y);
    }

    public IVector WithY(double y)
    {
        return new Vector(X, y);
    }

    public static bool operator ==(Vector left, Vector right)
    {
        return left.X == right.X && left.Y == right.Y;
    }

    public static bool operator !=(Vector left, Vector right)
    {
        return !(left == right);
    }

    public override bool Equals(object obj)
    {
        return obj is Vector vector && this == vector;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}