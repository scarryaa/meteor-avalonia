using meteor.Core.Models.EventArgs;

namespace meteor.Core.Interfaces.Services.Editor;

public interface IEditorInputHandler
{
    void HandleKeyDown(KeyEventArgs e);
    void HandleTextInput(TextInputEventArgs e);
}