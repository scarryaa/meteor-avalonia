using meteor.Core.Enums;

namespace meteor.Core.Models.Events;

public class KeyEventArgs
{
    public Key Key { get; set; }
    public KeyModifiers Modifiers { get; set; }

    public KeyEventArgs()
    {
    }

    public KeyEventArgs(Key key, KeyModifiers? modifiers = null)
    {
        Key = key;
        Modifiers = modifiers ?? KeyModifiers.None;
    }
}