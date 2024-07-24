namespace meteor.Core.Interfaces.Services;

public interface ITextMeasurer
{
    (double Width, double Height) Measure(string text);
}