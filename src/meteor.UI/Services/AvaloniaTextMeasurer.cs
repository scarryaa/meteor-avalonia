using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Services;

public class AvaloniaTextMeasurer : ITextMeasurer
{
    private readonly Typeface _typeface;
    private readonly double _fontSize;
    private readonly Dictionary<string, TextLayout> _textLayoutCache;

    public AvaloniaTextMeasurer(Typeface typeface, double fontSize)
    {
        _typeface = typeface;
        _fontSize = fontSize;
        _textLayoutCache = new Dictionary<string, TextLayout>();
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
        var textLayout = GetOrCreateTextLayout(text);
        return textLayout.WidthIncludingTrailingWhitespace;
    }

    public double GetStringHeight(string text)
    {
        var textLayout = GetOrCreateTextLayout(text);
        return textLayout.Height;
    }

    public double GetLineHeight()
    {
        return GetOrCreateTextLayout("X").Height;
    }

    public double GetCharWidth()
    {
        return GetOrCreateTextLayout("X").WidthIncludingTrailingWhitespace;
    }

    public (int line, int column) GetLineAndColumnFromIndex(string text, int index)
    {
        var lines = text.Split('\n');
        var currentIndex = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (currentIndex + lines[i].Length >= index) return (i, index - currentIndex);
            currentIndex += lines[i].Length + 1;
        }

        return (lines.Length - 1, lines[lines.Length - 1].Length);
    }

    public int GetIndexFromLineAndColumn(string text, int line, int column)
    {
        var lines = text.Split('\n');
        var index = 0;
        for (var i = 0; i < line && i < lines.Length; i++) index += lines[i].Length + 1;
        return index + Math.Min(column, lines[Math.Min(line, lines.Length - 1)].Length);
    }

    private TextLayout GetOrCreateTextLayout(string text)
    {
        if (!_textLayoutCache.TryGetValue(text, out var textLayout))
        {
            textLayout = CreateTextLayout(text);
            _textLayoutCache[text] = textLayout;
        }

        return textLayout;
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
    }
}