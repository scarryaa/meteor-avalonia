namespace meteor.Core.Models;

public struct Size
{
    public double Width { get; set; }
    public double Height { get; set; }

    public Size(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public override string ToString()
    {
        return $"Width: {Width}, Height: {Height}";
    }

    public override bool Equals(object obj)
    {
        if (obj is Size size) return Width == size.Width && Height == size.Height;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public static bool operator ==(Size left, Size right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Size left, Size right)
    {
        return !(left == right);
    }
}