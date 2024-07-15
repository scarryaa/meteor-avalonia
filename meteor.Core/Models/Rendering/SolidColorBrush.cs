using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Rendering;

public class SolidColorBrush(Color color, double opacity = 1.0) : IBrush
{
    public Color Color { get; private set; } = color;
    public double Opacity { get; set; } = opacity;

    public void SetColor(Color color)
    {
        Color = color;
    }
}