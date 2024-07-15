using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using meteor.App.Models;
using meteor.Core.Interfaces.Rendering;
using Color = Avalonia.Media.Color;
using FontStyle = Avalonia.Media.FontStyle;
using FontWeight = Avalonia.Media.FontWeight;
using FormattedText = meteor.Core.Models.Rendering.FormattedText;
using IBrush = meteor.Core.Interfaces.Rendering.IBrush;
using IImage = meteor.Core.Interfaces.Rendering.IImage;
using SolidColorBrush = meteor.Core.Models.Rendering.SolidColorBrush;

namespace meteor.App.Rendering;

public class AvaloniaImageFactory : IImageFactory
{
    public IImage CreateImageFromFormattedText(FormattedText text)
    {
        var avaloniaFormattedText = AvaloniaFormattedText.FromCore(text);
        var width = avaloniaFormattedText.Width;
        var height = avaloniaFormattedText.Height;
        var renderTarget = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(width), (int)Math.Ceiling(height)));

        using (var context = renderTarget.CreateDrawingContext())
        {
            context.DrawText(avaloniaFormattedText.AvaloniaText, new Point(0, 0));
        }

        return new AvaloniaImage(renderTarget);
    }

    public IImage CreateEmptyImage(double lineHeight)
    {
        var width = 1;
        var height = (int)Math.Ceiling(lineHeight);
        var renderTarget = new RenderTargetBitmap(new PixelSize(width, height));

        return new AvaloniaImage(renderTarget);
    }

    private static FontStyle ConvertToAvaloniaFontStyle(Core.Models.Rendering.FontStyle fontStyle)
    {
        return fontStyle switch
        {
            Core.Models.Rendering.FontStyle.Normal => FontStyle.Normal,
            Core.Models.Rendering.FontStyle.Italic => FontStyle.Italic,
            Core.Models.Rendering.FontStyle.Oblique => FontStyle.Oblique,
            _ => FontStyle.Normal
        };
    }

    private static FontWeight ConvertToAvaloniaFontWeight(Core.Models.Rendering.FontWeight fontWeight)
    {
        return fontWeight switch
        {
            Core.Models.Rendering.FontWeight.Thin => FontWeight.Thin,
            Core.Models.Rendering.FontWeight.ExtraLight => FontWeight.ExtraLight,
            Core.Models.Rendering.FontWeight.Light => FontWeight.Light,
            Core.Models.Rendering.FontWeight.Normal => FontWeight.Normal,
            Core.Models.Rendering.FontWeight.Medium => FontWeight.Medium,
            Core.Models.Rendering.FontWeight.SemiBold => FontWeight.SemiBold,
            Core.Models.Rendering.FontWeight.Bold => FontWeight.Bold,
            Core.Models.Rendering.FontWeight.ExtraBold => FontWeight.ExtraBold,
            Core.Models.Rendering.FontWeight.Black => FontWeight.Black,
            _ => FontWeight.Normal
        };
    }

    private static Avalonia.Media.IBrush ConvertToAvaloniaBrush(IBrush brush)
    {
        if (brush is SolidColorBrush solidColorBrush)
            return new Avalonia.Media.SolidColorBrush(Color.FromArgb(
                solidColorBrush.Color.A,
                solidColorBrush.Color.R,
                solidColorBrush.Color.G,
                solidColorBrush.Color.B
            ));

        // Handle other brush types or return a default brush
        return Brushes.Black;
    }
}