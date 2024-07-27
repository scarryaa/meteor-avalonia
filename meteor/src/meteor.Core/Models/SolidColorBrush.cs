using meteor.Core.Interfaces.Models;

namespace meteor.Core.Models;

public class SolidColorBrush : ISolidColorBrush
{
    public Color Color { get; }

    public SolidColorBrush(Color color)
    {
        Color = color;
    }
}