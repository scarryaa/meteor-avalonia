using meteor.Core.Models;
using meteor.Core.Models.EventArgs;
using KeyEventArgs = meteor.Core.Models.EventArgs.KeyEventArgs;

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
        : base(KeyConverter.Convert(e.Key), KeyConverter.Convert(e.KeyModifiers))
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
        : base(KeyConverter.Convert(avaloniaKeyEventArgs.Key), KeyConverter.Convert(avaloniaKeyEventArgs.KeyModifiers))
    {
    }

    public static KeyEventArgs Convert(Avalonia.Input.KeyEventArgs avaloniaKeyEventArgs)
    {
        return new KeyEventArgsAdapter(avaloniaKeyEventArgs);
    }
}

public static class SizeAdapter
{
    public static Size Convert(Avalonia.Size avaloniaSize)
    {
        return new Size(avaloniaSize.Width, avaloniaSize.Height);
    }
}