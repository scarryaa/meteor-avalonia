namespace meteor.Core.Models.Rendering;

public struct Color(byte r, byte g, byte b, byte a = 255)
{
    public byte R { get; set; } = r;
    public byte G { get; set; } = g;
    public byte B { get; set; } = b;
    public byte A { get; set; } = a;
}