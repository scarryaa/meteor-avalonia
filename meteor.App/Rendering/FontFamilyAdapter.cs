using Avalonia.Media;
using meteor.Core.Interfaces;

namespace meteor.App.Rendering;

public class FontFamilyAdapter : IFontFamily
{
    private readonly FontFamily _avaloniaFontFamily;

    public FontFamilyAdapter(FontFamily avaloniaFontFamily)
    {
        _avaloniaFontFamily = avaloniaFontFamily;
    }

    public string Name => _avaloniaFontFamily.Name;

    public FontFamily ToAvaloniaFontFamily()
    {
        return _avaloniaFontFamily;
    }
}