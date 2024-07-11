using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace meteor.Models;

public class BatchingDrawingContext
{
    private readonly List<Action<DrawingContext>> _drawActions = new();

    public void DrawLine(Pen pen, Point p1, Point p2)
    {
        _drawActions.Add(ctx => ctx.DrawLine(pen, p1, p2));
    }

    public void DrawRectangle(IBrush brush, IPen pen, Rect rect)
    {
        _drawActions.Add(ctx => ctx.DrawRectangle(brush, pen, rect));
    }

    public void DrawText(IBrush foreground, Point origin, FormattedText text)
    {
        _drawActions.Add(ctx => ctx.DrawText(text, origin));
    }

    public void DrawImage(IImage source, Rect destRect)
    {
        _drawActions.Add(ctx => ctx.DrawImage(source, destRect));
    }

    public void PushClip(Rect clip)
    {
        _drawActions.Add(ctx => ctx.PushClip(clip));
    }

    public void FillRectangle(IBrush brush, Rect rect)
    {
        _drawActions.Add(ctx => ctx.FillRectangle(brush, rect));
    }

    public void Flush(DrawingContext actualContext)
    {
        foreach (var action in _drawActions) action(actualContext);
        _drawActions.Clear();
    }
}