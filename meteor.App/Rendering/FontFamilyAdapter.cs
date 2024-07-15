using Avalonia.Media;
using meteor.Core.Interfaces;

namespace meteor.App.Rendering;

public class FontFamilyAdapter(FontFamily avaloniaFontFamily) : IFontFamily
{
    public string Name => avaloniaFontFamily.Name;

    public FontFamily ToAvaloniaFontFamily()
    {
        return avaloniaFontFamily;
    }
}