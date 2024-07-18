using System;
using System.Linq;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Services;

public class AvaloniaEditorSizeCalculator : IEditorSizeCalculator
{
    private readonly ITextMeasurer _textMeasurer;
    private readonly double _cachedLineHeight;
    private int _cachedLineCount;
    private double _cachedMaxLineWidth;
    private double _windowWidth;
    private double _windowHeight;

    public AvaloniaEditorSizeCalculator(ITextMeasurer textMeasurer)
    {
        _textMeasurer = textMeasurer;
        _cachedLineHeight = _textMeasurer.GetStringHeight("X");
    }

    public (double width, double height) CalculateEditorSize(string text, double availableWidth, double availableHeight)
    {
        var lines = text.Split('\n');
        var lineCount = lines.Length;

        if (lineCount != _cachedLineCount)
        {
            _cachedMaxLineWidth = lines.Length > 0
                ? Math.Max(availableWidth, lines.Max(line => _textMeasurer.GetStringWidth(line)))
                : availableWidth;
            _cachedLineCount = lineCount;
        }

        var contentWidth = Math.Max(_cachedMaxLineWidth, availableWidth);
        var contentHeight = Math.Max(_cachedLineHeight * lineCount, availableHeight);

        return (contentWidth, contentHeight);
    }

    public void UpdateWindowSize(double width, double height)
    {
        _windowWidth = width;
        _windowHeight = height;
        InvalidateCache();
    }

    public void InvalidateCache()
    {
        _cachedLineCount = 0;
        _cachedMaxLineWidth = 0;
    }
}