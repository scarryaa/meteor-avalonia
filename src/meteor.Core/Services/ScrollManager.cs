using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class ScrollManager : IScrollManager
{
    private const int ScrollMargin = 50; // pixels
    private const int DebounceDelay = 50; // milliseconds
    private readonly ITabService _tabService;
    private readonly ITextMeasurer _textMeasurer;
    private DateTime _lastScrollTime = DateTime.MinValue;

    public ScrollManager(ITabService tabService, ITextMeasurer textMeasurer)
    {
        _tabService = tabService;
        _textMeasurer = textMeasurer;
    }

    public async Task<Vector> CalculateScrollOffsetAsync(int cursorPosition, double editorWidth, double editorHeight,
        double viewportWidth, double viewportHeight, Vector currentScrollOffset, int textLength)
    {
        if (DateTime.UtcNow - _lastScrollTime < TimeSpan.FromMilliseconds(DebounceDelay)) return currentScrollOffset;

        _lastScrollTime = DateTime.Now;

        return await Task.Run(() => CalculateScrollOffset(cursorPosition, editorWidth, editorHeight,
            viewportWidth, viewportHeight, currentScrollOffset, textLength));
    }

    private Vector CalculateScrollOffset(int cursorPosition, double editorWidth, double editorHeight,
        double viewportWidth, double viewportHeight, Vector currentScrollOffset, int textLength)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            Console.WriteLine("Warning: Invalid viewport dimensions in ScrollManager.");
            return currentScrollOffset;
        }

        try
        {
            var activeTextBufferService = _tabService.GetActiveTextBufferService();
            var actualTextLength = activeTextBufferService.Length;

            if (actualTextLength != textLength)
                Console.WriteLine(
                    $"Warning: Mismatch between provided textLength ({textLength}) and actual TextBufferService length ({actualTextLength})");

            if (cursorPosition < 0 || cursorPosition > actualTextLength)
            {
                Console.WriteLine($"Warning: Invalid cursor position {cursorPosition}. Using clamped value.");
                cursorPosition = Math.Clamp(cursorPosition, 0, actualTextLength);
            }

            var (cursorX, cursorY) = activeTextBufferService.CalculatePositionFromIndex(cursorPosition, _textMeasurer);

            var newScrollX = CalculateScrollDimension(cursorX, currentScrollOffset.X, viewportWidth, editorWidth);
            var newScrollY = CalculateScrollDimension(cursorY, currentScrollOffset.Y, viewportHeight, editorHeight);

            return new Vector(newScrollX, newScrollY);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ScrollManager.CalculateScrollOffset: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return currentScrollOffset;
        }
    }

    private static double CalculateScrollDimension(double cursorPos, double currentScroll, double viewportSize,
        double editorSize)
    {
        if (cursorPos < currentScroll + ScrollMargin)
            return Math.Max(0, cursorPos - ScrollMargin);
        if (cursorPos > currentScroll + viewportSize - ScrollMargin)
            return Math.Min(Math.Max(0, editorSize - viewportSize), cursorPos - viewportSize + ScrollMargin);
        return currentScroll;
    }
}