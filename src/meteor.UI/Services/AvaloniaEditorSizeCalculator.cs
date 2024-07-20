using System;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Services;

public class AvaloniaEditorSizeCalculator : IEditorSizeCalculator
{
    private const int ChunkSize = 4096;
    private readonly char[] _buffer = new char[ChunkSize];
    private readonly double _cachedLineHeight;
    private readonly ITextMeasurer _textMeasurer;
    private int _cachedLineCount;
    private double _cachedMaxLineWidth;
    private int _cachedTextLength; // Length of the text buffer
    private double _windowHeight;
    private double _windowWidth;

    public AvaloniaEditorSizeCalculator(ITextMeasurer textMeasurer)
    {
        _textMeasurer = textMeasurer;
        _cachedLineHeight = _textMeasurer.GetLineHeight();
    }

    public (double width, double height) CalculateEditorSize(ITextBufferService textBufferService,
        double windowWidth, double windowHeight)
    {
        if (textBufferService.Length != _cachedTextLength)
            UpdateCache(textBufferService);

        var contentWidth = Math.Max(_cachedMaxLineWidth, _windowWidth);
        var contentHeight = Math.Max(_cachedLineHeight * _cachedLineCount, _windowHeight);

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
        _cachedTextLength = -1;
        _cachedLineCount = 0;
        _cachedMaxLineWidth = 0;
    }

    private void UpdateCache(ITextBufferService textBufferService)
    {
        _cachedLineCount = 0;
        _cachedMaxLineWidth = 0;
        var bufferIndex = 0;
        var lineStartIndex = 0;

        void ProcessLine()
        {
            if (bufferIndex > lineStartIndex)
            {
                var lineWidth = _textMeasurer.GetStringWidth(_buffer, lineStartIndex, bufferIndex - lineStartIndex);
                if (lineWidth > _cachedMaxLineWidth)
                    _cachedMaxLineWidth = lineWidth;
            }
            
            _cachedLineCount++;
            lineStartIndex = bufferIndex;
        }

        textBufferService.Iterate(c =>
        {
            if (bufferIndex == ChunkSize)
            {
                ProcessLine();
                bufferIndex = 0;
                lineStartIndex = 0;
            }

            if (c == '\n')
            {
                ProcessLine();
                bufferIndex = 0;
                lineStartIndex = 0;
            }
            else
            {
                _buffer[bufferIndex++] = c;
            }
        });

        ProcessLine();

        _cachedTextLength = textBufferService.Length;
    }
}