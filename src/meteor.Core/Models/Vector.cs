namespace meteor.Core.Models;

public struct Vector : IEquatable<Vector>
{
    public double X { get; set; }
    public double Y { get; set; }

    public Vector(double x, double y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector vector && Equals(vector);
    }

    public bool Equals(Vector other)
    {
        const double epsilon = 1e-10;
        return Math.Abs(X - other.X) < epsilon && Math.Abs(Y - other.Y) < epsilon;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Vector left, Vector right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(Vector left, Vector right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}