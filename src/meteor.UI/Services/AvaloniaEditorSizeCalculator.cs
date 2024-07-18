using System;
using System.Text;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Services;

public class AvaloniaEditorSizeCalculator : IEditorSizeCalculator
{
    private readonly ITextMeasurer _textMeasurer;
    private readonly double _cachedLineHeight;
    private int _cachedLineCount;
    private double _cachedMaxLineWidth;
    private int _cachedTextLength;
    private double _windowWidth;
    private double _windowHeight;
    private readonly StringBuilder _stringBuilder = new();

    public AvaloniaEditorSizeCalculator(ITextMeasurer textMeasurer)
    {
        _textMeasurer = textMeasurer;
        _cachedLineHeight = _textMeasurer.GetStringHeight("X");
    }

    public (double width, double height) CalculateEditorSize(ITextBufferService textBufferService,
        double availableWidth, double availableHeight)
    {
        if (textBufferService.Length != _cachedTextLength) UpdateCache(textBufferService);

        var contentWidth = Math.Max(_cachedMaxLineWidth, _windowWidth);
        var contentHeight = Math.Max(_cachedLineHeight * _cachedLineCount, _windowHeight);

        Console.WriteLine($"Content size: {contentWidth}x{contentHeight}");
        return (contentWidth, contentHeight);
    }

    private void UpdateCache(ITextBufferService textBufferService)
    {
        _cachedLineCount = 0;
        _cachedMaxLineWidth = 0;
        _stringBuilder.Clear();

        textBufferService.Iterate(c =>
        {
            if (c == '\n')
            {
                UpdateLine(_stringBuilder.ToString());
                _stringBuilder.Clear();
                _cachedLineCount++;
            }
            else
            {
                _stringBuilder.Append(c);
            }
        });

        if (_stringBuilder.Length > 0)
        {
            UpdateLine(_stringBuilder.ToString());
            _cachedLineCount++;
        }

        _cachedTextLength = textBufferService.Length;
    }

    private void UpdateLine(string line)
    {
        var lineWidth = _textMeasurer.GetStringWidth(line);
        if (lineWidth > _cachedMaxLineWidth) _cachedMaxLineWidth = lineWidth;
    }

    public void UpdateWindowSize(double width, double height)
    {
        _windowWidth = width;
        _windowHeight = height;
        InvalidateCache();
    }

    public void InvalidateCache()
    {
        _cachedTextLength = -1;
        _cachedLineCount = 0;
        _cachedMaxLineWidth = 0;
    }
}
