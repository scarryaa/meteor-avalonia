using meteor.Core.Models.EventArgs;

namespace meteor.Core.Interfaces.ViewModels;

public interface IEditorViewModel
{
    string Content { get; }
    int CursorPosition { get; }

    void HandleKeyDown(KeyEventArgs e);
    void HandleTextInput(TextInputEventArgs e);
}