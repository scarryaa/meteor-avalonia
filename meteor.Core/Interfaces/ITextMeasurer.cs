namespace meteor.Core.Interfaces;

public interface ITextMeasurer
{
    double MeasureWidth(string text, double fontSize, string fontFamily);
    double MeasureHeight(string text, double fontSize, string fontFamily);
    double GetLineHeight(double fontSize, string fontFamily);
}