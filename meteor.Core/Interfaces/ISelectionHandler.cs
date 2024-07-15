namespace meteor.Core.Interfaces;

public interface ISelectionHandler
{
    bool HasSelection { get; }
    int SelectionAnchor { get; }
    int SelectionStart { get; }
    int SelectionEnd { get; }
    bool IsSelecting { get; }

    void StartSelection(int position);
    void UpdateSelectionDuringDrag(int position, bool isDoubleClick, bool isTripleClick);
    void EndSelection();
    void ClearSelection();
    void SelectAll();
    void SelectWord(int position);
    void SelectLine(int position);
}