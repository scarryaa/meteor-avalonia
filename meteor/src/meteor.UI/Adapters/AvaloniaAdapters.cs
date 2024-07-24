using meteor.Core.Models.EventArgs;
using Key = meteor.Core.Enums.Key;
using KeyEventArgs = meteor.Core.Models.EventArgs.KeyEventArgs;
using KeyModifiers = meteor.Core.Enums.KeyModifiers;

namespace meteor.UI.Adapters;

public class TextInputEventArgsAdapter : TextInputEventArgs
{
    private readonly Avalonia.Input.TextInputEventArgs _originalArgs;

    public TextInputEventArgsAdapter(Avalonia.Input.TextInputEventArgs args)
        : base(args.Text)
    {
        _originalArgs = args;
    }

    public static TextInputEventArgs Convert(Avalonia.Input.TextInputEventArgs args)
    {
        return new TextInputEventArgsAdapter(args);
    }
}

public class KeyDownEventArgsAdapter : KeyEventArgs
{
    public KeyDownEventArgsAdapter(Avalonia.Input.KeyEventArgs e)
        : base((Key)e.Key, (KeyModifiers)e.KeyModifiers)
    {
    }

    public static KeyEventArgs Convert(Avalonia.Input.KeyEventArgs e)
    {
        return new KeyDownEventArgsAdapter(e);
    }
}

public class KeyEventArgsAdapter : KeyEventArgs
{
    public KeyEventArgsAdapter(Avalonia.Input.KeyEventArgs avaloniaKeyEventArgs)
        : base((Key)avaloniaKeyEventArgs.Key, (KeyModifiers)avaloniaKeyEventArgs.KeyModifiers)
    {
    }

    public static KeyEventArgs Convert(Avalonia.Input.KeyEventArgs avaloniaKeyEventArgs)
    {
        return new KeyEventArgsAdapter(avaloniaKeyEventArgs);
    }
}