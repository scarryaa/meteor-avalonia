using Avalonia.Media;
using ISolidColorBrush = meteor.Core.Interfaces.Models.ISolidColorBrush;

namespace meteor.UI.Common.Extensions;

public static class SolidColorBrushExtensions
{
    public static SolidColorBrush ToAvaloniaColor(this ISolidColorBrush brush)
    {
        return new SolidColorBrush(new Color(
            brush.Color.A,
            brush.Color.R,
            brush.Color.G,
            brush.Color.B
        ));
    }
}