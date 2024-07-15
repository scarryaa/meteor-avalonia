using System;
using Avalonia.Media;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Models.Rendering;
using IBrush = meteor.Core.Interfaces.Rendering.IBrush;
using IImage = meteor.Core.Interfaces.Rendering.IImage;
using IPen = meteor.Core.Interfaces.Rendering.IPen;

namespace meteor.App.Rendering;

public class AvaloniaDrawingContext : IDrawingContext
{
    private readonly DrawingContext _context;

    public AvaloniaDrawingContext(DrawingContext context)
    {
        _context = context;
    }

    public void FillRectangle(IBrush brush, Rect rect)
    {
        _context.FillRectangle(ConvertBrush(brush), ConvertRect(rect));
    }

    public void DrawLine(IPen pen, Point start, Point end)
    {
        _context.DrawLine(ConvertPen(pen), ConvertPoint(start), ConvertPoint(end));
    }

    public void DrawText(IFormattedText formattedText, Point origin)
    {
        _context.DrawText(ConvertFormattedText(formattedText), ConvertPoint(origin));
    }

    public void DrawImage(IImage image, Rect destRect)
    {
        _context.DrawImage(ConvertImage(image), ConvertRect(destRect));
    }

    public void PushClip(Rect clipRect)
    {
        _context.PushClip(ConvertRect(clipRect));
    }

    public void PopClip()
    {
        // Do nothing
    }

    private Avalonia.Media.IBrush ConvertBrush(IBrush brush)
    {
        // Implement brush conversion
        throw new NotImplementedException();
    }

    private Avalonia.Media.IPen ConvertPen(IPen pen)
    {
        // Implement pen conversion
        throw new NotImplementedException();
    }

    private FormattedText ConvertFormattedText(IFormattedText formattedText)
    {
        // Implement formatted text conversion
        throw new NotImplementedException();
    }

    private Avalonia.Media.IImage ConvertImage(IImage image)
    {
        // Implement image conversion
        throw new NotImplementedException();
    }

    private Avalonia.Rect ConvertRect(Rect rect)
    {
        return new Avalonia.Rect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    private Avalonia.Point ConvertPoint(Point point)
    {
        return new Avalonia.Point(point.X, point.Y);
    }
}