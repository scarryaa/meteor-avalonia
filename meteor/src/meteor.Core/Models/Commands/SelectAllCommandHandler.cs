using meteor.Core.Enums;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Models.EventArgs;

namespace meteor.Core.Models.Commands;

public class SelectAllCommandHandler : ISelectAllCommandHandler
{
    public bool IsSelectAllCommand(KeyEventArgs e)
    {
        return (e.Key == Key.A || e.Key == Key.C) &&
               (e.Modifiers.HasFlag(KeyModifiers.Control) || e.Modifiers.HasFlag(KeyModifiers.Meta));
    }
}