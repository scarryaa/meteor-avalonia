using System.Globalization;
using Avalonia.Media;
using meteor.Core.Interfaces.Rendering;
using FontStyle = meteor.Core.Models.Rendering.FontStyle;
using FontWeight = meteor.Core.Models.Rendering.FontWeight;
using IBrush = meteor.Core.Interfaces.Rendering.IBrush;
using SolidColorBrush = meteor.Core.Models.Rendering.SolidColorBrush;

namespace meteor.App.Rendering;

public class AvaloniaFormattedText : IFormattedText
{
    public AvaloniaFormattedText(
        FormattedText avaloniaFormattedText,
        string fontFamily,
        FontStyle fontStyle,
        FontWeight fontWeight,
        double fontSize,
        IBrush foreground)
    {
        AvaloniaText = avaloniaFormattedText;
        FontFamily = fontFamily;
        FontStyle = fontStyle;
        FontWeight = fontWeight;
        FontSize = fontSize;
        Foreground = foreground;
    }

    public string? Text { get; }
    public string FontFamily { get; }
    public FontStyle FontStyle { get; }
    public FontWeight FontWeight { get; }
    public double FontSize { get; }
    public IBrush Foreground { get; }

    public double Width => AvaloniaText.Width;
    public double Height => AvaloniaText.Height;

    public FormattedText AvaloniaText { get; }

    public static AvaloniaFormattedText FromCore(Core.Models.Rendering.FormattedText coreFormattedText)
    {
        var avaloniaFormattedText = new FormattedText(
            coreFormattedText.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(coreFormattedText.FontFamily),
            13,
            Brushes.Black
        );
        avaloniaFormattedText.SetForegroundBrush(ConvertToAvaloniaBrush(coreFormattedText.Foreground));

        return new AvaloniaFormattedText(
            avaloniaFormattedText,
            coreFormattedText.FontFamily,
            coreFormattedText.FontStyle,
            coreFormattedText.FontWeight,
            coreFormattedText.FontSize,
            coreFormattedText.Foreground
        );
    }

    private static Avalonia.Media.FontStyle ConvertToAvaloniaFontStyle(FontStyle fontStyle)
    {
        return fontStyle switch
        {
            FontStyle.Italic => Avalonia.Media.FontStyle.Italic,
            FontStyle.Oblique => Avalonia.Media.FontStyle.Oblique,
            _ => Avalonia.Media.FontStyle.Normal
        };
    }

    private static Avalonia.Media.FontWeight ConvertToAvaloniaFontWeight(FontWeight fontWeight)
    {
        return fontWeight switch
        {
            FontWeight.Thin => Avalonia.Media.FontWeight.Thin,
            FontWeight.ExtraLight => Avalonia.Media.FontWeight.ExtraLight,
            FontWeight.Light => Avalonia.Media.FontWeight.Light,
            FontWeight.Normal => Avalonia.Media.FontWeight.Normal,
            FontWeight.Medium => Avalonia.Media.FontWeight.Medium,
            FontWeight.SemiBold => Avalonia.Media.FontWeight.SemiBold,
            FontWeight.Bold => Avalonia.Media.FontWeight.Bold,
            FontWeight.ExtraBold => Avalonia.Media.FontWeight.ExtraBold,
            FontWeight.Black => Avalonia.Media.FontWeight.Black,
            _ => Avalonia.Media.FontWeight.Normal
        };
    }

    private static Avalonia.Media.IBrush ConvertToAvaloniaBrush(IBrush brush)
    {
        if (brush is SolidColorBrush solidColorBrush)
            return new Avalonia.Media.SolidColorBrush(new Color(
                solidColorBrush.Color.A,
                solidColorBrush.Color.R,
                solidColorBrush.Color.G,
                solidColorBrush.Color.B
            ));

        // Default to a black brush if conversion is not possible
        return Brushes.Black;
    }
}