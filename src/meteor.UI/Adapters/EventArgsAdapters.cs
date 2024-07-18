using Avalonia;
using KeyEventArgs = meteor.Core.Models.Events.KeyEventArgs;
using PointerEventArgs = meteor.Core.Models.Events.PointerEventArgs;
using PointerPressedEventArgs = meteor.Core.Models.Events.PointerPressedEventArgs;
using PointerReleasedEventArgs = meteor.Core.Models.Events.PointerReleasedEventArgs;
using TextInputEventArgs = meteor.Core.Models.Events.TextInputEventArgs;

namespace meteor.UI.Adapters;

public static class EventArgsAdapters
{
    public static PointerPressedEventArgs ToPointerPressedEventArgs(Avalonia.Input.PointerPressedEventArgs e,
        Visual control)
    {
        var point = e.GetCurrentPoint(control);
        var index = ConvertPointToIndex(point.Position);
        return new PointerPressedEventArgs(index);
    }

    public static PointerReleasedEventArgs ToPointerReleasedEventArgs(Avalonia.Input.PointerReleasedEventArgs e,
        Visual control)
    {
        var point = e.GetCurrentPoint(control);
        var index = ConvertPointToIndex(point.Position);
        return new PointerReleasedEventArgs(index);
    }

    public static PointerEventArgs ToPointerEventArgsModel(Avalonia.Input.PointerEventArgs e, Visual control)
    {
        var point = e.GetCurrentPoint(control);
        var index = ConvertPointToIndex(point.Position);
        return new PointerEventArgs((int)point.Position.X, (int)point.Position.Y, index);
    }

    public static TextInputEventArgs ToTextInputEventArgsModel(Avalonia.Input.TextInputEventArgs e)
    {
        return new TextInputEventArgs(e.Text);
    }

    public static KeyEventArgs ToKeyEventArgsModel(KeyEventArgs e)
    {
        return new KeyEventArgs(e.Key, e.Modifiers);
    }

    private static int ConvertPointToIndex(Point point)
    {
        // TODO implement conversion logic to get index from point coordinates
        return (int)(point.X + point.Y);
    }
}