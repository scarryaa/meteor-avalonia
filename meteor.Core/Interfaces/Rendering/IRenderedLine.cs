using meteor.Core.Models.Rendering;

namespace meteor.Core.Interfaces.Rendering;

public interface IRenderedLine
{
    IImage Image { get; }
    void Invalidate();
    void Render(IDrawingContext context, Rect position);
}