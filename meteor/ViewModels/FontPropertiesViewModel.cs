using Avalonia.Media;
using ReactiveUI;

namespace meteor.ViewModels;

public class FontPropertiesViewModel : ViewModelBase
{
    private FontFamily _fontFamily = new("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono");
    private double _fontSize = 13;
    private double _lineHeight;

    public FontPropertiesViewModel()
    {
        _lineHeight = CalculateLineHeight(_fontSize);
    }

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set => this.RaiseAndSetIfChanged(ref _fontFamily, value);
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            // Recalculate line height whenever font size changes
            this.RaiseAndSetIfChanged(ref _fontSize, value);
            LineHeight = CalculateLineHeight(value);
        }
    }

    public double LineHeight
    {
        get => _lineHeight;
        set => this.RaiseAndSetIfChanged(ref _lineHeight, value);
    }

    private double CalculateLineHeight(double fontSize)
    {
        return fontSize * 1.2;
    }
}