using meteor.Core.Models.Rendering;

namespace meteor.Core.Interfaces.Rendering;

public interface IBrush
{
    Color Color { get; }
    double Opacity { get; set; }
    void SetColor(Color color);
}