using Avalonia.Input;
using meteor.Core.Interfaces.Events;
using meteor.Core.Interfaces.Rendering;

namespace meteor.App.Adapters;

public class PointerEventArgsAdapter : IPointerEventArgs
{
    private readonly PointerEventArgs _args;

    public PointerEventArgsAdapter(PointerEventArgs args)
    {
        _args = args;
    }

    public double X => _args.GetPosition(null).X;
    public double Y => _args.GetPosition(null).Y;
    public int ClickCount => 0;

    public bool Handled
    {
        get => _args.Handled;
        set => _args.Handled = value;
    }

    public IPoint GetPosition()
    {
        return new PointAdapter(_args.GetPosition(null));
    }
}