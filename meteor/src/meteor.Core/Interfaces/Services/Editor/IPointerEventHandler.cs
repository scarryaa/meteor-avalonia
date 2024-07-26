using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services.Editor;

public interface IPointerEventHandler
{
    void HandlePointerPressed(Point point);
    void HandlePointerMoved(Point point);
    void HandlePointerReleased();
}