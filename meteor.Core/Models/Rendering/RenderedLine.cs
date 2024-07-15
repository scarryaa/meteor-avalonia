using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class RenderedTextLine : IRenderedLine
{
    public IImage Image { get; private set; }
    private readonly FormattedText _formattedText;
    private readonly IImageFactory _imageFactory;

    public RenderedTextLine(IImageFactory imageFactory, FormattedText formattedText)
    {
        _imageFactory = imageFactory;
        _formattedText = formattedText;
        RenderImage();
    }

    private void RenderImage()
    {
        Image = CreateImageFromFormattedText(_formattedText);
    }

    public void Invalidate()
    {
        RenderImage();
    }

    public void Render(IDrawingContext context, Rect bounds)
    {
        if (Image != null) context.DrawImage(Image, bounds);
    }

    private IImage CreateImageFromFormattedText(FormattedText text)
    {
        return _imageFactory.CreateImageFromFormattedText(text);
    }
}