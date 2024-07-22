using meteor.Core.Enums;
using meteor.Core.Models.Events;

namespace meteor.Core.Interfaces.Services;

public interface IInputService
{
    void InsertText(string text);
    void DeleteText(int index, int length);
    Task HandleKeyDown(Key key, KeyModifiers? modifiers = null);
    int GetNewCursorPosition(Key key, int currentPosition);
    void HandlePointerPressed(PointerPressedEventArgs e);
    void HandlePointerMoved(PointerEventArgs e);
    void HandlePointerReleased(PointerReleasedEventArgs e);
    void HandleTextInput(TextInputEventArgs e);
}