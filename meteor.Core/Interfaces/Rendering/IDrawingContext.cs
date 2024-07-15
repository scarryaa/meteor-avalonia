using meteor.Core.Models.Rendering;

namespace meteor.Core.Interfaces.Rendering;

public interface IDrawingContext
{
    void FillRectangle(IBrush brush, Rect rect);
    void DrawLine(IPen pen, Point start, Point end);
    void DrawText(IFormattedText formattedText, Point origin);
    void DrawImage(IImage image, Rect destRect);
    void PushClip(Rect clipRect);
    void PopClip();
}