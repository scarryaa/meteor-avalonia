using meteor.Core.Models.Rendering;

namespace meteor.Core.Interfaces.Rendering;

public interface IFormattedText
{
    string Text { get; }
    string FontFamily { get; }
    FontStyle FontStyle { get; }
    FontWeight FontWeight { get; }
    double FontSize { get; }
    IBrush Foreground { get; }
}