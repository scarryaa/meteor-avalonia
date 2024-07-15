using System.Globalization;
using Avalonia.Media;
using meteor.Core.Interfaces;

namespace meteor.App.Services;

public class AvaloniaTextMeasurer : ITextMeasurer
{
    public double MeasureWidth(string text, double fontSize, string fontFamily)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(fontFamily),
            fontSize,
            Brushes.Black
        );
        return formattedText.WidthIncludingTrailingWhitespace;
    }

    public double MeasureHeight(string text, double fontSize, string fontFamily)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(fontFamily),
            fontSize,
            Brushes.Black
        );

        return formattedText.Height;
    }

    public double GetLineHeight(double fontSize, string fontFamily)
    {
        var formattedText = new FormattedText(
            "Aj",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(fontFamily),
            fontSize,
            Brushes.Black
        );

        return formattedText.Height;
    }
}