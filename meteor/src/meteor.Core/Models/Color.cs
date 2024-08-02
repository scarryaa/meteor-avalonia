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

        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => $"{c}{c}"));
        }

        if (hex.Length == 6)
        {
            hex = "FF" + hex;
        }
        else if (hex.Length != 8)
        {
            throw new ArgumentException("Invalid hex color format. Expected 3, 6, or 8 characters.", nameof(hex));
        }

        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hexValue))
        {
            throw new ArgumentException("Invalid hex color format.", nameof(hex));
        }

        return new Color(
            (byte)((hexValue >> 24) & 0xFF),
            (byte)((hexValue >> 16) & 0xFF),
            (byte)((hexValue >> 8) & 0xFF),
            (byte)(hexValue & 0xFF)
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