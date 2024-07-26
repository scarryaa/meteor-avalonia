using meteor.Core.Models.EventArgs;

namespace meteor.Core.Interfaces.Commands;

public interface IModifierKeyHandler
{
    bool IsModifierOrPageKey(KeyEventArgs e);
}