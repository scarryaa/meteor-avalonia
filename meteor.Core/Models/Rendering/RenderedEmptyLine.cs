using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class RenderedEmptyLine : IRenderedLine
{
    public IImage Image { get; private set; }
    private readonly double _lineHeight;
    private readonly IImageFactory _imageFactory;

    public RenderedEmptyLine(IImageFactory imageFactory, double lineHeight)
    {
        _imageFactory = imageFactory;
        _lineHeight = lineHeight;
        RenderImage();
    }

    private void RenderImage()
    {
        Image = CreateEmptyImage(_lineHeight);
    }

    public void Invalidate()
    {
        RenderImage();
    }

    public void Render(IDrawingContext context, Rect bounds)
    {
        if (Image != null) context.DrawImage(Image, bounds);
    }

    private IImage CreateEmptyImage(double lineHeight)
    {
        return _imageFactory.CreateEmptyImage(lineHeight);
    }
}