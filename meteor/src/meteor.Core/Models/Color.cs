using System.Globalization;

namespace meteor.Core.Models;

public struct Color
{
    public byte A { get; set; }
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public Color(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public static Color Black => new(255, 0, 0, 0);
    public static Color White => new(255, 255, 255, 255);
    public static Color Red => new(255, 255, 0, 0);
    public static Color Green => new(255, 0, 255, 0);
    public static Color Blue => new(255, 0, 0, 255);
    public static Color Transparent => new(0, 0, 0, 0);

    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
            hex = "FF" + hex;
        return new Color(
            byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
            byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
            byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber),
            byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber)
        );
    }

    public static Color FromRgb(byte r, byte g, byte b)
    {
        return new Color(255, r, g, b);
    }

    public static Color FromArgb(byte a, byte r, byte g, byte b)
    {
        return new Color(a, r, g, b);
    }
}