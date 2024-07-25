using System.Globalization;
using Avalonia.Media;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Services;

public class AvaloniaTextMeasurer : ITextMeasurer
{
    public (double Width, double Height) MeasureText(string text, string fontFamily, double fontSize)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(fontFamily),
            fontSize,
            Brushes.Black);

        return (formattedText.Width, formattedText.Height);
    }

    public double GetLineHeight(string fontFamily, double fontSize)
    {
        var formattedText = new FormattedText(
            "Tg",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(fontFamily),
            fontSize,
            Brushes.Black);

        return formattedText.Height;
    }
}