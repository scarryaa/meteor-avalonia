using meteor.Core.Models.Rendering;

namespace meteor.Core.Interfaces.Rendering;

public interface IImageFactory
{
    IImage CreateImageFromFormattedText(FormattedText text);
    IImage CreateEmptyImage(double lineHeight);
}