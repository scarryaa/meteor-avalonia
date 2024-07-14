using System;
using Avalonia.Media.Imaging;

namespace meteor.Views.Models;

public class RenderedLine
{
    public WriteableBitmap Image { get; private set; }
    public double HorizontalOffset { get; private set; }
    public double Width { get; private set; }
    public bool IsInvalidated { get; private set; }

    public RenderedLine Update(WriteableBitmap image, double horizontalOffset, double width)
    {
        Image = image;
        HorizontalOffset = horizontalOffset;
        Width = width;
        IsInvalidated = false;
        return this;
    }

    public void CopyTo(RenderedLine other)
    {
        other.Image = Image;
        other.HorizontalOffset = HorizontalOffset;
        other.Width = Width;
        other.IsInvalidated = IsInvalidated;
    }

    public bool NeedsUpdate(double currentHorizontalOffset, double currentWidth)
    {
        return IsInvalidated ||
               Math.Abs(HorizontalOffset - currentHorizontalOffset) > 1e-6 ||
               Math.Abs(Width - currentWidth) > 1e-6;
    }

    public void Invalidate()
    {
        IsInvalidated = true;
    }
}