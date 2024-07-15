using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class SolidColorBrush : IBrush
{
    public Color Color { get; private set; }
    public double Opacity { get; set; }

    public SolidColorBrush(Color color, double opacity = 1.0)
    {
        Color = color;
        Opacity = opacity;
    }

    public void SetColor(Color color)
    {
        Color = color;
    }
}