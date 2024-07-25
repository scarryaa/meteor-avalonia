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

    public event EventHandler<Vector>? ScrollChanged;

    public ScrollManager(IEditorConfig config, ITextMeasurer textMeasurer)
    {
        _config = config;
        _textMeasurer = textMeasurer;
        LineHeight = _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize) * _config.LineHeightMultiplier;
    }

    public void UpdateViewportAndExtentSizes(Size viewport, Size extent)
    {
        Console.WriteLine($"UpdateViewportAndExtentSizes: Viewport = {viewport}, Extent = {extent}");
        Viewport = viewport;
        ExtentSize = extent;
    }

    public double LineHeight { get; }

    public Vector ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            if (_scrollOffset != value)
            {
                Console.WriteLine($"ScrollOffset changing from {_scrollOffset} to {value}");
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
            if (_viewport != value)
            {
                Console.WriteLine($"Viewport set: {value}");
                _viewport = value;
            }
        }
    }

    public Size ExtentSize
    {
        get => _extentSize;
        set
        {
            if (_extentSize != value)
            {
                Console.WriteLine($"ExtentSize set: {value}");
                _extentSize = value;
            }
        }
    }

    public void ScrollToLine(int lineNumber)
    {
        var newY = lineNumber * LineHeight;
        Console.WriteLine($"Scrolling to line {lineNumber}, new Y offset: {newY}");
        ScrollOffset = new Vector(ScrollOffset.X, newY);
    }

    public void ScrollToPosition(Vector position)
    {
        Console.WriteLine($"Scrolling to position {position}");
        ScrollOffset = position;
    }

    public void ScrollVertically(double delta)
    {
        var newY = Math.Max(0, ScrollOffset.Y + delta);
        Console.WriteLine($"Scrolling vertically by {delta}, new Y offset: {newY}");
        ScrollOffset = new Vector(ScrollOffset.X, newY);
    }

    public void ScrollHorizontally(double delta)
    {
        var newX = Math.Max(0, ScrollOffset.X + delta);
        Console.WriteLine($"Scrolling horizontally by {delta}, new X offset: {newX}");
        ScrollOffset = new Vector(newX, ScrollOffset.Y);
    }

    public void ScrollUp(int lines = 1)
    {
        Console.WriteLine($"Scrolling up by {lines} lines");
        ScrollVertically(-lines * LineHeight);
    }

    public void ScrollDown(int lines = 1)
    {
        Console.WriteLine($"Scrolling down by {lines} lines");
        ScrollVertically(lines * LineHeight);
    }

    public void PageUp()
    {
        Console.WriteLine("Page Up");
        ScrollVertically(-_viewport.Height);
    }

    public void PageDown()
    {
        Console.WriteLine("Page Down");
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

    public void EnsureLineIsVisible(int lineNumber)
    {
        var lineTop = lineNumber * LineHeight;
        var lineBottom = (lineNumber + 1) * LineHeight;

        if (lineTop < ScrollOffset.Y)
        {
            Console.WriteLine($"Line {lineNumber} is above the visible area, scrolling to line");
            ScrollToLine(lineNumber);
        }
        else if (lineBottom > ScrollOffset.Y + _viewport.Height)
        {
            Console.WriteLine($"Line {lineNumber} is below the visible area, scrolling to line");
            ScrollToLine(lineNumber - GetVisibleLineCount() + 1);
        }
    }
}