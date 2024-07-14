using System.Globalization;
using Avalonia;
using Avalonia.Media;
using meteor.core.Models;
using meteor.rendering.Models;

namespace meteor.rendering.Services;

public class RenderManager
{
    private readonly TextBuffer _textBuffer;
    private readonly LineCache _lineCache;
    private readonly SyntaxHighlighter _syntaxHighlighter;
    private int _visibleLineCount;
    private int _firstVisibleLine;
    private Size _lastSize;
    private double _lastScrollOffset;

    public RenderManager(TextBuffer textBuffer, SyntaxHighlighter syntaxHighlighter, double lineHeight,
        double charWidth)
    {
        _textBuffer = textBuffer;
        _syntaxHighlighter = syntaxHighlighter;
        _lineCache = new LineCache();
        LineHeight = lineHeight;
        CharWidth = charWidth;
    }

    public double LineHeight { get; set; }
    public double CharWidth { get; set; }

    public event EventHandler InvalidateRequested;

    public void UpdateViewport(Size size)
    {
        if (_lastSize != size)
        {
            _lastSize = size;
            _visibleLineCount = (int)(size.Height / LineHeight) + 1;
            RequestInvalidate();
        }
    }

    public void SetScrollPosition(double verticalOffset)
    {
        if (Math.Abs(_lastScrollOffset - verticalOffset) > 0.001)
        {
            _lastScrollOffset = verticalOffset;
            _firstVisibleLine = (int)(verticalOffset / LineHeight);
            RequestInvalidate();
        }
    }

    public void Invalidate(int lineStart = -1, int lineEnd = -1)
    {
        if (lineStart == -1) lineStart = 0;
        if (lineEnd == -1) lineEnd = _textBuffer.LineCount - 1;

        _lineCache.Invalidate(lineStart, lineEnd);
        RequestInvalidate();
    }

    public void Render(DrawingContext context, Size size)
    {
        Console.WriteLine(
            $"Render called: Size = {size}, FirstVisibleLine = {_firstVisibleLine}, VisibleLineCount = {_visibleLineCount}");

        context.DrawRectangle(Brushes.White, null, new Rect(new Point(0, 0), size));

        for (var i = 0; i < _visibleLineCount; i++)
        {
            var lineIndex = _firstVisibleLine + i;

            if (lineIndex < 0 || lineIndex >= _textBuffer.LineCount)
                continue;

            var line = _textBuffer.LineAt(lineIndex);
            var lineText = _textBuffer.GetText(line.Start, line.Length);

            Console.WriteLine($"Rendering line {lineIndex}: {lineText}");

            var cachedLine = _lineCache.GetLine(lineIndex, lineText);
            if (cachedLine == null)
            {
                var highlightedSegments = _syntaxHighlighter.Highlight(lineText);
                cachedLine = new CachedLine(lineText, highlightedSegments);
                _lineCache.CacheLine(lineIndex, cachedLine);
            }

            var y = i * LineHeight;
            RenderLine(context, cachedLine, new Point(0, y));
        }
    }

    private void RenderLine(DrawingContext context, CachedLine cachedLine, Point origin)
    {
        var x = origin.X;
        foreach (var segment in cachedLine.HighlightedSegments)
        {
            var formattedText = new FormattedText(
                segment.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas"),
                13,
                Brushes.Black
            );

            Console.WriteLine($"Drawing text: {segment.Text} at ({x}, {origin.Y})");

            context.DrawText(formattedText, new Point(x, origin.Y));
            x += segment.Text.Length * CharWidth;
        }
    }

    private void RequestInvalidate()
    {
        Console.WriteLine("Invalidate requested");
        InvalidateRequested?.Invoke(this, EventArgs.Empty);
    }
}