using Avalonia.Input;
using meteor.Core.Interfaces.Events;

namespace meteor.App.Adapters;

public class PointerReleasedEventArgsAdapter : PointerEventArgsAdapter, IPointerReleasedEventArgs
{
    private readonly PointerReleasedEventArgs _args;

    public PointerReleasedEventArgsAdapter(PointerReleasedEventArgs args)
        : base(args)
    {
        _args = args;
    }

    public int ClickCount => _args.InitialPressMouseButton == MouseButton.Left ? 1 : 0;
}