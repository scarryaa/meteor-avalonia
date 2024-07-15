using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class FormattedText : IFormattedText
{
    public string Text { get; }
    public string FontFamily { get; }
    public FontStyle FontStyle { get; }
    public FontWeight FontWeight { get; }
    public double FontSize { get; }
    public IBrush Foreground { get; }

    public FormattedText(string text, string fontFamily, FontStyle fontStyle, FontWeight fontWeight, double fontSize,
        IBrush foreground)
    {
        Text = text;
        FontFamily = fontFamily;
        FontStyle = fontStyle;
        FontWeight = fontWeight;
        FontSize = fontSize;
        Foreground = foreground;
    }
}