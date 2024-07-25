using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services;

public interface IScrollManager
{
    double GutterWidth { get; set; }
    Vector ScrollOffset { get; set; }
    Size Viewport { get; set; }
    Size ExtentSize { get; set; }
    double LineHeight { get; }

    void ScrollToLine(int lineNumber);
    void ScrollToPosition(Vector position);
    void ScrollVertically(double delta);
    void ScrollHorizontally(double delta);
    void ScrollUp(int lines = 1);
    void ScrollDown(int lines = 1);
    void PageUp();
    void PageDown();
    int GetVisibleLineCount();
    bool IsLineVisible(int lineNumber);
    void EnsureLineIsVisible(int lineNumber, double cursorX);
    void UpdateViewportAndExtentSizes(Size viewport, Size extent);

    event EventHandler<Vector> ScrollChanged;
}