using Avalonia.Input;
using meteor.Core.Interfaces.Events;

namespace meteor.App.Adapters;

public class PointerReleasedEventArgsAdapter(PointerReleasedEventArgs args)
    : PointerEventArgsAdapter(args), IPointerReleasedEventArgs
{
    public int ClickCount => args.InitialPressMouseButton == MouseButton.Left ? 1 : 0;
}