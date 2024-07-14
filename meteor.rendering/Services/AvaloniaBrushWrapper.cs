using IBrush = meteor.core.Models.IBrush;

namespace meteor.rendering.Services;

public class AvaloniaBrushWrapper : IBrush
{
    public Avalonia.Media.IBrush AvaloniaBrush { get; }

    public AvaloniaBrushWrapper(Avalonia.Media.IBrush avaloniaBrush)
    {
        AvaloniaBrush = avaloniaBrush;
    }
}