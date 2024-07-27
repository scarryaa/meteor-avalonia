using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.EventArgs;

namespace meteor.Core.Interfaces.Services.Editor;

public interface IEditorInputHandler
{
    void HandleKeyDown(IEditorViewModel viewModel, KeyEventArgs e);
    void HandleTextInput(IEditorViewModel viewModel, TextInputEventArgs e);
}