using meteor.Core.Interfaces.Events;

namespace meteor.Core.Interfaces;

public interface IInputManager : IDisposable
{
    void OnPointerPressed(IPointerPressedEventArgs e);
    void OnPointerMoved(IPointerEventArgs e);
    void OnPointerReleased(IPointerReleasedEventArgs e);
    Task OnKeyDown(IKeyEventArgs e);
    void OnTextInput(ITextInputEventArgs e);

    bool IsTripleClickDrag { get; }
    bool IsDoubleClickDrag { get; }
    object? VisualReference { get; set; }
}