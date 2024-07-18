using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.UI.Services;

public class AvaloniaTextMeasurer : ITextMeasurer
{
    private readonly Typeface _typeface;
    private readonly double _fontSize;
    private readonly LruCache<string, TextLayout> _textLayoutCache;
    private readonly TextLayout _singleCharLayout;
    private readonly Dictionary<char, double> _charWidthCache;

    private const int MaxCacheSize = 1000;
    private const int ChunkSize = 100;

    public AvaloniaTextMeasurer(Typeface typeface, double fontSize)
    {
        _typeface = typeface;
        _fontSize = fontSize;
        _textLayoutCache = new LruCache<string, TextLayout>(MaxCacheSize);
        _singleCharLayout = CreateTextLayout("X");
        _charWidthCache = new Dictionary<char, double>();
    }

    public int GetIndexAtPosition(string text, double x, double y)
    {
        var textLayout = GetOrCreateTextLayout(text);
        var hitTestResult = textLayout.HitTestPoint(new Point(x, y));
        return hitTestResult.TextPosition;
    }

    public (double x, double y) GetPositionAtIndex(string text, int index)
    {
        var textLayout = GetOrCreateTextLayout(text);
        var textBounds = textLayout.HitTestTextPosition(index);
        return (textBounds.X, textBounds.Y);
    }

    public double GetStringWidth(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (text.Length == 1) return GetCharWidth(text[0]);

        return GetStringWidth(text.ToCharArray(), 0, text.Length);
    }

    public double GetStringWidth(char[] buffer, int start, int length)
    {
        if (length == 0) return 0;
        if (length == 1) return GetCharWidth(buffer[start]);

        double width = 0;
        for (var i = start; i < start + length; i++) width += GetCharWidth(buffer[i]);
        return width;
    }

    public double GetStringHeight(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return _singleCharLayout.Height * text.Split('\n').Length;
    }

    public double GetLineHeight()
    {
        return _singleCharLayout.Height;
    }

    public double GetCharWidth()
    {
        return _singleCharLayout.WidthIncludingTrailingWhitespace;
    }

    private double GetCharWidth(char c)
    {
        if (_charWidthCache.TryGetValue(c, out var width)) return width;

        var layout = CreateTextLayout(c.ToString());
        width = layout.WidthIncludingTrailingWhitespace;
        _charWidthCache[c] = width;
        return width;
    }

    public (int line, int column) GetLineAndColumnFromIndex(string text, int index)
    {
        var line = 0;
        var currentIndex = 0;

        while (currentIndex < index)
        {
            var nextNewLine = text.IndexOf('\n', currentIndex);
            if (nextNewLine == -1 || nextNewLine >= index)
                return (line, index - currentIndex);

            line++;
            currentIndex = nextNewLine + 1;
        }

        return (line, 0);
    }

    public int GetIndexFromLineAndColumn(string text, int line, int column)
    {
        var currentLine = 0;
        var currentIndex = 0;

        while (currentLine < line && currentIndex < text.Length)
        {
            var nextNewLine = text.IndexOf('\n', currentIndex);
            if (nextNewLine == -1)
                return Math.Min(currentIndex + column, text.Length);

            currentLine++;
            currentIndex = nextNewLine + 1;
        }

        return Math.Min(currentIndex + column, text.Length);
    }

    private TextLayout GetOrCreateTextLayout(string text)
    {
        if (text.Length <= ChunkSize)
        {
            if (!_textLayoutCache.TryGet(text, out var textLayout))
            {
                textLayout = CreateTextLayout(text);
                _textLayoutCache.Add(text, textLayout);
            }

            return textLayout;
        }

        return CreateTextLayout(text);
    }

    private TextLayout CreateTextLayout(string text)
    {
        return new TextLayout(
            text,
            _typeface,
            _fontSize,
            Brushes.Black,
            TextAlignment.Left,
            TextWrapping.NoWrap,
            TextTrimming.None
        );
    }

    public void ClearCache()
    {
        _textLayoutCache.Clear();
        _charWidthCache.Clear();
    }
}