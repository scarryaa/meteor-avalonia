using meteor.Core.Enums;

namespace meteor.Core.Models.EventArgs;

public class KeyEventArgs : System.EventArgs
{
    public Key Key { get; }
    public KeyModifiers Modifiers { get; }

    public KeyEventArgs(Key key, KeyModifiers modifiers)
    {
        Key = key;
        Modifiers = modifiers;
    }
}