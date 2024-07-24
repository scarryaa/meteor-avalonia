using meteor.Core.Models.EventArgs;

namespace meteor.Core.Interfaces.Services;

public interface IInputManager
{
    void HandleKeyDown(KeyEventArgs e);
    void HandleTextInput(TextInputEventArgs e);
}