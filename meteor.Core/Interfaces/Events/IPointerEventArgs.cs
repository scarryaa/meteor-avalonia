using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Interfaces.Events;

public interface IPointerEventArgs
{
    IPoint GetPosition();
    bool Handled { get; set; }
}