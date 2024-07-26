using System.Globalization;
using System.Text;
using Avalonia.Media;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.UI.Config;

namespace meteor.UI.Services;

public class AvaloniaTextMeasurer : ITextMeasurer
{
    private readonly Dictionary<(string FontFamily, double FontSize), Typeface> _typefaceCache = new();
    private readonly Dictionary<(string FontFamily, double FontSize), double> _lineHeightCache = new();
    private readonly IEditorConfig _config;
    private readonly AvaloniaEditorConfig _avaloniaConfig;

    public AvaloniaTextMeasurer(IEditorConfig config)
    {
        _config = config;
        _avaloniaConfig = new AvaloniaEditorConfig();
    }
    
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

        return (formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
    }

    public (double Width, double Height) MeasureText(StringBuilder stringBuilder, string fontFamily, double fontSize)
    {
        var typeface = GetOrCreateTypeface(fontFamily, fontSize);

        var formattedText = new FormattedText(
            stringBuilder.ToString(),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.Black);

        return (formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
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
            typeface = _avaloniaConfig.Typeface;
            _typefaceCache[key] = typeface;
        }

        return typeface;
    }
}