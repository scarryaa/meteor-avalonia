using Avalonia.Input;
using meteor.Core.Interfaces.Events;

namespace meteor.App.Adapters;

public class PointerPressedEventArgsAdapter(PointerPressedEventArgs args)
    : PointerEventArgsAdapter(args), IPointerPressedEventArgs
{
    public int ClickCount => args.ClickCount;
}