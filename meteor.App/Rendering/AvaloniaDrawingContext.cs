using System;
using System.Globalization;
using Avalonia.Media;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Models.Rendering;
using Color = meteor.Core.Models.Rendering.Color;
using FormattedText = Avalonia.Media.FormattedText;
using IBrush = meteor.Core.Interfaces.Rendering.IBrush;
using IImage = meteor.Core.Interfaces.Rendering.IImage;
using IPen = meteor.Core.Interfaces.Rendering.IPen;
using Pen = Avalonia.Media.Pen;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;

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
        if (brush is Core.Models.Rendering.SolidColorBrush solidColorBrush)
            return new SolidColorBrush(ConvertColor(solidColorBrush.Color));
        if (brush is BrushAdapter brushAdapter)
            return brushAdapter.ToAvaloniaBrush();
        throw new NotSupportedException($"Brush type {brush.GetType()} is not supported.");
    }
    
    private Avalonia.Media.IPen ConvertPen(IPen pen)
    {
        return new Pen(
            ConvertBrush(pen.Brush),
            pen.Thickness,
            new DashStyle(pen.DashArray, pen.DashOffset)
        );
    }

    private FormattedText ConvertFormattedText(IFormattedText formattedText)
    {
        return new FormattedText(
            formattedText.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(formattedText.FontFamily),
            formattedText.FontSize,
            ConvertBrush(formattedText.Foreground)
        );
    }

    private Avalonia.Media.IImage ConvertImage(IImage image)
    {
        if (image is Avalonia.Media.IImage avaloniaImage) return avaloniaImage;
        // If it's not already an Avalonia image, you might need to create one from raw data
        throw new NotSupportedException($"Image type {image.GetType()} is not supported.");
    }

    private Avalonia.Media.Color ConvertColor(Color color)
    {
        return new Avalonia.Media.Color(color.A, color.R, color.G, color.B);
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