using Avalonia.Input;
using meteor.Core.Interfaces.Events;

namespace meteor.App.Adapters;

public class PointerPressedEventArgsAdapter : PointerEventArgsAdapter, IPointerPressedEventArgs
{
    private readonly PointerPressedEventArgs _args;

    public PointerPressedEventArgsAdapter(PointerPressedEventArgs args)
        : base(args)
    {
        _args = args;
    }

    public int ClickCount => _args.ClickCount;
}