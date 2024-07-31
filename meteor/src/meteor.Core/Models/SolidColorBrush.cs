using meteor.Core.Interfaces.Models;

namespace meteor.Core.Models;

public class SolidColorBrush : ISolidColorBrush
{
    public SolidColorBrush(Color color)
    {
        Color = color;
    }

    public Color Color { get; }
}