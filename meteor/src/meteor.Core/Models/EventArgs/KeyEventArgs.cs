using meteor.Core.Enums;

namespace meteor.Core.Models.EventArgs;

public class KeyEventArgs : System.EventArgs
{
    public Key Key { get; init; }
    public KeyModifiers Modifiers { get; init; }
    public bool Handled { get; set; } = false;

    public KeyEventArgs(Key key, KeyModifiers modifiers)
    {
        Key = key;
        Modifiers = modifiers;
    }

    public KeyEventArgs()
    {
    }
}