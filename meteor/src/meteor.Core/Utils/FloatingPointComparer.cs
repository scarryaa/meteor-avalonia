using meteor.Core.Models;

namespace meteor.Core.Utils;

public class FloatingPointComparer
{
    public static bool AreEqual(double a, double b, double epsilon = 1e-9)
    {
        return Math.Abs(a - b) <= epsilon;
    }

    public static bool AreEqual(Vector a, Vector b, double epsilon = 1e-9)
    {
        return AreEqual(a.X, b.X, epsilon) && AreEqual(a.Y, b.Y, epsilon);
    }
}