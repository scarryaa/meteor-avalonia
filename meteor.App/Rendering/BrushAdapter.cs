using Avalonia.Media;
using IBrush = meteor.Core.Interfaces.Rendering.IBrush;

namespace meteor.App.Rendering;

public class BrushAdapter : IBrush
{
    private readonly SolidColorBrush _avaloniaBrush;

    public BrushAdapter(SolidColorBrush avaloniaBrush)
    {
        _avaloniaBrush = avaloniaBrush;
    }

    public Core.Models.Rendering.Color Color => new(_avaloniaBrush.Color.A, _avaloniaBrush.Color.R,
        _avaloniaBrush.Color.G,
        _avaloniaBrush.Color.B);

    public double Opacity
    {
        get => _avaloniaBrush.Opacity;
        set => _avaloniaBrush.Opacity = value;
    }

    public void SetColor(Core.Models.Rendering.Color color)
    {
        _avaloniaBrush.Color = new Color(color.A, color.R, color.G, color.B);
    }

    public void SetColor(Color color)
    {
        _avaloniaBrush.Color = new Color(color.A, color.R, color.G, color.B);
    }

    public SolidColorBrush ToAvaloniaBrush()
    {
        return _avaloniaBrush;
    }
}