namespace meteor.Core.Models;

public struct Vector
{
    public double X { get; set; }
    public double Y { get; set; }

    public Vector(double x, double y)
    {
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return $"X: {X}, Y: {Y}";
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector vector) return X == vector.X && Y == vector.Y;
        return false;
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
}