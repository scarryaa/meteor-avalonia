using meteor.Core.Enums;

namespace meteor.Core.Interfaces.Events;

public interface IKeyEventArgs
{
    Key Key { get; }
    bool IsShiftPressed { get; }
    bool IsControlPressed { get; }
    bool Handled { get; set; }
}