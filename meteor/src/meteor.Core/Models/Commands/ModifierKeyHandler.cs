using meteor.Core.Enums;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Models.EventArgs;

namespace meteor.Core.Models.Commands;

public class ModifierKeyHandler : IModifierKeyHandler
{
    public bool IsModifierOrPageKey(KeyEventArgs e)
    {
        return e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
               e.Key == Key.LeftShift || e.Key == Key.RightShift ||
               e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
               e.Key == Key.PageUp || e.Key == Key.PageDown ||
               e.Modifiers.HasFlag(KeyModifiers.Alt) ||
               e.Modifiers.HasFlag(KeyModifiers.Control) ||
               e.Modifiers.HasFlag(KeyModifiers.Meta) ||
               e.Modifiers.HasFlag(KeyModifiers.Shift);
    }
}