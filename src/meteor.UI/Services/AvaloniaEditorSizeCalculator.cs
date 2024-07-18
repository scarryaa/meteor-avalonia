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
    private string _cachedText = string.Empty;
    private double _windowWidth;
    private double _windowHeight;

    public AvaloniaEditorSizeCalculator(ITextMeasurer textMeasurer)
    {
        _textMeasurer = textMeasurer;
        _cachedLineHeight = _textMeasurer.GetStringHeight("X");
    }

    public (double width, double height) CalculateEditorSize(ITextBufferService textBufferService,
        double availableWidth, double availableHeight)
    {
        var text = textBufferService.GetText();
        if (text != _cachedText) UpdateCache(textBufferService);

        var contentWidth = Math.Max(_cachedMaxLineWidth, _windowWidth);
        var contentHeight = Math.Max(_cachedLineHeight * _cachedLineCount, _windowHeight);

        Console.WriteLine($"Content size: {contentWidth}x{contentHeight}");
        return (contentWidth, contentHeight);
    }

    private void UpdateCache(ITextBufferService textBufferService)
    {
        _cachedLineCount = 0;
        _cachedMaxLineWidth = 0;
        var sb = new StringBuilder();

        for (var i = 0; i < textBufferService.Length; i++)
        {
            var c = textBufferService[i];
            if (c == '\n')
            {
                UpdateLine(sb.ToString());
                sb.Clear();
                _cachedLineCount++;
            }
            else
            {
                sb.Append(c);
            }
        }

        if (sb.Length > 0)
        {
            UpdateLine(sb.ToString());
            _cachedLineCount++;
        }

        _cachedText = textBufferService.GetText();
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
        _cachedText = string.Empty;
        _cachedLineCount = 0;
        _cachedMaxLineWidth = 0;
    }
}