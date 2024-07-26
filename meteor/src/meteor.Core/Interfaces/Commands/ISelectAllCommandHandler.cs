using meteor.Core.Models.EventArgs;

namespace meteor.Core.Interfaces.Commands;

public interface ISelectAllCommandHandler
{
    bool IsSelectAllCommand(KeyEventArgs e);
}