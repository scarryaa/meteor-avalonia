using Avalonia.Input;
using meteor.Core.Interfaces.Events;

namespace meteor.App.Adapters;

public class TextInputEventArgsAdapter : ITextInputEventArgs
{
    private readonly TextInputEventArgs _args;

    public TextInputEventArgsAdapter(TextInputEventArgs args)
    {
        _args = args;
    }

    public string Text => _args.Text;

    public bool Handled
    {
        get => _args.Handled;
        set => _args.Handled = value;
    }
}