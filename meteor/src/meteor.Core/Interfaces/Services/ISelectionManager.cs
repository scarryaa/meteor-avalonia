using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services;

public interface ISelectionManager
{
    Selection CurrentSelection { get; }
    bool HasSelection { get; }
    event EventHandler SelectionChanged;
    void SetSelection(int start, int end);
    void ClearSelection();
    string GetSelectedText(ITextBufferService textBufferService);
    void ExtendSelection(int newPosition);
    void StartSelection(int position);
    void UpdateSelection(int position, bool isShiftPressed);
    void HandleMouseSelection(int position, bool isShiftPressed);
    void HandleKeyboardSelection(int position, bool isShiftPressed);
}