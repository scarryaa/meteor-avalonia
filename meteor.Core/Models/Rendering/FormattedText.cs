using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class FormattedText(
    string? text,
    string fontFamily,
    FontStyle fontStyle,
    FontWeight fontWeight,
    double fontSize,
    IBrush foreground)
    : IFormattedText
{
    public string? Text { get; } = text;
    public string FontFamily { get; } = fontFamily;
    public FontStyle FontStyle { get; } = fontStyle;
    public FontWeight FontWeight { get; } = fontWeight;
    public double FontSize { get; } = fontSize;
    public IBrush Foreground { get; } = foreground;
}