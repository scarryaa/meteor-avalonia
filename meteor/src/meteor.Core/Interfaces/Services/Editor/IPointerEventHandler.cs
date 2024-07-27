using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services.Editor;

public interface IPointerEventHandler
{
    void HandlePointerPressed(IEditorViewModel viewModel, Point point);
    void HandlePointerMoved(IEditorViewModel viewModel, Point point);
    void HandlePointerReleased(IEditorViewModel viewModel);
}