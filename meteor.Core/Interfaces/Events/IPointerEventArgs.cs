using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Interfaces.Events;

public interface IPointerEventArgs
{
    double X { get; }
    double Y { get; }
    int ClickCount { get; }
    bool Handled { get; set; }
    IPoint? GetPosition(object? relativeTo = null);
}