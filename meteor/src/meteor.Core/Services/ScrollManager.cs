using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class ScrollManager : IScrollManager
{
    private readonly IEditorConfig _config;
    private readonly ITextMeasurer _textMeasurer;
    private Vector _scrollOffset;
    private Size _viewport;
    private Size _extentSize;
    private double _gutterWidth;

    public event EventHandler<Vector>? ScrollChanged;

    public ScrollManager(IEditorConfig config, ITextMeasurer textMeasurer)
    {
        _config = config;
        _textMeasurer = textMeasurer;
        LineHeight = _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize) * _config.LineHeightMultiplier;
    }

    public void UpdateViewportAndExtentSizes(Size viewport, Size extent)
    {
        Viewport = viewport;
        ExtentSize = extent;
    }

    public double LineHeight { get; }

    public double GutterWidth
    {
        get => _gutterWidth;
        set
        {
            if (_gutterWidth != value)
            {
                _gutterWidth = value;
                UpdateViewportAndExtentSizes(Viewport, ExtentSize);
            }
        }
    }

    public Vector ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            if (_scrollOffset != value)
            {
                _scrollOffset = value;
                ScrollChanged?.Invoke(this, value);
            }
        }
    }

    public Size Viewport
    {
        get => _viewport;
        set
        {
            if (_viewport != value) _viewport = value;
        }
    }

    public Size ExtentSize
    {
        get => _extentSize;
        set
        {
            if (_extentSize != value) _extentSize = value;
        }
    }

    public void ScrollToLine(int lineNumber)
    {
        var newY = lineNumber * LineHeight;
        ScrollOffset = new Vector(ScrollOffset.X, newY);
    }

    public void ScrollToPosition(Vector position)
    {
        ScrollOffset = position;
    }

    public void ScrollVertically(double delta)
    {
        var newY = Math.Max(0, ScrollOffset.Y + delta);
        ScrollOffset = new Vector(ScrollOffset.X, newY);
    }

    public void UpdateMaxLineWidth(double maxLineWidth)
    {
        ExtentSize = new Size(maxLineWidth + GutterWidth, ExtentSize.Height);
    }

    public void ScrollHorizontally(double delta)
    {
        var newX = Math.Max(0, ScrollOffset.X + delta);
        ScrollOffset = new Vector(newX, ScrollOffset.Y);
    }

    public void ScrollUp(int lines = 1)
    {
        ScrollVertically(-lines * LineHeight);
    }

    public void ScrollDown(int lines = 1)
    {
        ScrollVertically(lines * LineHeight);
    }

    public void PageUp()
    {
        ScrollVertically(-_viewport.Height);
    }

    public void PageDown()
    {
        ScrollVertically(_viewport.Height);
    }

    public int GetVisibleLineCount()
    {
        return (int)Math.Ceiling(_viewport.Height / LineHeight);
    }

    public bool IsLineVisible(int lineNumber)
    {
        var lineTop = lineNumber * LineHeight;
        return lineTop >= ScrollOffset.Y && lineTop < ScrollOffset.Y + _viewport.Height;
    }

    public void EnsureLineIsVisible(int lineNumber, double cursorX, bool isSelection = false)
    {
        var lineTop = lineNumber * LineHeight;
        var lineBottom = (lineNumber + 1) * LineHeight;

        // Vertical scrolling
        var verticalMargin = isSelection ? 0 : LineHeight * 3;
        if (lineTop < ScrollOffset.Y + verticalMargin)
            ScrollToLine(lineNumber - (isSelection ? 0 : 3));
        else if (lineBottom > ScrollOffset.Y + _viewport.Height - verticalMargin)
            ScrollToLine(lineNumber - GetVisibleLineCount() + (isSelection ? 1 : 4));

        // Horizontal scrolling
        var leftMargin = 50;
        var rightMargin = 50;
        var effectiveViewportWidth = _viewport.Width - GutterWidth;
        var effectiveCursorX = cursorX - GutterWidth;

        var adjustedScrollOffsetX = ScrollOffset.X;
        if (effectiveCursorX < ScrollOffset.X + leftMargin)
            adjustedScrollOffsetX = Math.Max(0, effectiveCursorX - leftMargin);
        else if (effectiveCursorX > ScrollOffset.X + effectiveViewportWidth - rightMargin)
            adjustedScrollOffsetX = effectiveCursorX - effectiveViewportWidth + rightMargin;

        if (adjustedScrollOffsetX != ScrollOffset.X)
            ScrollOffset = new Vector(adjustedScrollOffsetX, ScrollOffset.Y);
    }
}