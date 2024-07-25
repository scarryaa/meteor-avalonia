using meteor.Core.Models.EventArgs;

namespace meteor.Core.Interfaces.Services;

public interface IInputManager
{
    Task HandleKeyDown(KeyEventArgs e);
    void HandleTextInput(TextInputEventArgs e);
}