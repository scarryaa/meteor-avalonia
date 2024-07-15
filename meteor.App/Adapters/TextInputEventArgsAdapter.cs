using Avalonia.Input;
using meteor.Core.Interfaces.Events;

namespace meteor.App.Adapters;

public class TextInputEventArgsAdapter(TextInputEventArgs args) : ITextInputEventArgs
{
    public string Text => args.Text;

    public bool Handled
    {
        get => args.Handled;
        set => args.Handled = value;
    }
}