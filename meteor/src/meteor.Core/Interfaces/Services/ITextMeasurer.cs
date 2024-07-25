namespace meteor.Core.Interfaces.Services;

public interface ITextMeasurer
{
    (double Width, double Height) MeasureText(string text, string fontFamily, double fontSize);
    double GetLineHeight(string fontFamily, double fontSize);
}