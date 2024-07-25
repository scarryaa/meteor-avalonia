using System.Globalization;
using Avalonia.Media;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Services;

public class AvaloniaTextMeasurer : ITextMeasurer
{
    private readonly Dictionary<(string FontFamily, double FontSize), Typeface> _typefaceCache = new();
    private readonly Dictionary<(string FontFamily, double FontSize), double> _lineHeightCache = new();

    public (double Width, double Height) MeasureText(string text, string fontFamily, double fontSize)
    {
        var typeface = GetOrCreateTypeface(fontFamily, fontSize);

        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.Black);

        return (formattedText.Width, formattedText.Height);
    }

    public double GetLineHeight(string fontFamily, double fontSize)
    {
        var key = (fontFamily, fontSize);
        if (_lineHeightCache.TryGetValue(key, out var cachedHeight)) return cachedHeight;

        var typeface = GetOrCreateTypeface(fontFamily, fontSize);
        
        var formattedText = new FormattedText(
            "Tg",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.Black);

        var lineHeight = formattedText.Height;
        _lineHeightCache[key] = lineHeight;

        return lineHeight;
    }

    private Typeface GetOrCreateTypeface(string fontFamily, double fontSize)
    {
        var key = (fontFamily, fontSize);
        if (!_typefaceCache.TryGetValue(key, out var typeface))
        {
            typeface = new Typeface(fontFamily);
            _typefaceCache[key] = typeface;
        }

        return typeface;
    }
}