using Avalonia.Input;
using meteor.Core.Interfaces.Events;
using meteor.Core.Interfaces.Rendering;

namespace meteor.App.Adapters;

public class PointerEventArgsAdapter(PointerEventArgs args) : IPointerEventArgs
{
    public double X => args.GetPosition(null).X;
    public double Y => args.GetPosition(null).Y;
    public int ClickCount => 0;

    public bool Handled
    {
        get => args.Handled;
        set => args.Handled = value;
    }

    public IPoint? GetPosition()
    {
        return new PointAdapter(args.GetPosition(null));
    }
}