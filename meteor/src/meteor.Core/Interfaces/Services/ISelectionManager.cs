using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services;

public interface ISelectionManager
{
    event EventHandler SelectionChanged;
    
    Selection CurrentSelection { get; }
    bool HasSelection { get; }
    void SetSelection(int start, int end);
    void ClearSelection();
    string GetSelectedText(ITextBufferService textBufferService);
    void ExtendSelection(int newPosition);
    void StartSelection(int position);
}