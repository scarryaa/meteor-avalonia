using System;
using Avalonia.Media.Imaging;

namespace meteor.Views.Models;

public class RenderedLine
{
    public WriteableBitmap Image { get; }
    public double HorizontalOffset { get; }
    public double Width { get; }

    public RenderedLine(WriteableBitmap image, double horizontalOffset, double width)
    {
        Image = image;
        HorizontalOffset = horizontalOffset;
        Width = width;
    }

    public bool NeedsUpdate(double currentHorizontalOffset, double currentWidth)
    {
        return Math.Abs(HorizontalOffset - currentHorizontalOffset) > 1 || Math.Abs(Width - currentWidth) > 1;
    }
}