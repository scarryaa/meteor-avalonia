using Avalonia.Media;
using IBrush = meteor.Core.Interfaces.Rendering.IBrush;

namespace meteor.App.Rendering;

public class BrushAdapter(SolidColorBrush avaloniaBrush) : IBrush
{
    public Core.Models.Rendering.Color Color => new(avaloniaBrush.Color.A, avaloniaBrush.Color.R,
        avaloniaBrush.Color.G,
        avaloniaBrush.Color.B);

    public double Opacity
    {
        get => avaloniaBrush.Opacity;
        set => avaloniaBrush.Opacity = value;
    }

    public void SetColor(Core.Models.Rendering.Color color)
    {
        avaloniaBrush.Color = new Color(color.A, color.R, color.G, color.B);
    }

    public void SetColor(Color color)
    {
        avaloniaBrush.Color = new Color(color.A, color.R, color.G, color.B);
    }

    public SolidColorBrush ToAvaloniaBrush()
    {
        return avaloniaBrush;
    }
}