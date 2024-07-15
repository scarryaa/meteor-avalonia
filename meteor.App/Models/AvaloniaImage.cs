using System;
using Avalonia.Media.Imaging;
using meteor.App.Rendering;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Models.Rendering;

namespace meteor.App.Models;

public class AvaloniaImage : IImage
{
    private readonly RenderTargetBitmap _image;

    public AvaloniaImage(RenderTargetBitmap image)
    {
        _image = image;
    }

    public ISize Size => new Core.Models.Rendering.Size(_image.PixelSize.Width, _image.PixelSize.Height);

    public void Draw(IDrawingContext context, Rect rect)
    {
        if (context is AvaloniaDrawingContext avaloniaContext)
            avaloniaContext.DrawImage(this, rect);
        else
            throw new ArgumentException("Unsupported drawing context type", nameof(context));
    }

    internal RenderTargetBitmap GetAvaloniaImage()
    {
        return _image;
    }
}