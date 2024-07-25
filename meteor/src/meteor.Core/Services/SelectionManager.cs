using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class SelectionManager : ISelectionManager
{
    private int _selectionAnchor;

    public Selection CurrentSelection { get; private set; }
    public bool HasSelection => CurrentSelection.Start != CurrentSelection.End;

    public SelectionManager()
    {
        CurrentSelection = new Selection(0, 0);
    }

    public void StartSelection(int position)
    {
        _selectionAnchor = position;
        CurrentSelection = new Selection(position, position);
    }

    public void SetSelection(int start, int end)
    {
        CurrentSelection = new Selection(Math.Min(start, end), Math.Max(start, end));
    }

    public void ClearSelection()
    {
        CurrentSelection = new Selection(0, 0);
    }

    public string GetSelectedText(ITextBufferService textBufferService)
    {
        if (!HasSelection)
            return string.Empty;

        return textBufferService.GetContentSlice(CurrentSelection.Start, CurrentSelection.End);
    }

    public void ExtendSelection(int newPosition)
    {
        CurrentSelection = new Selection(
            Math.Min(_selectionAnchor, newPosition),
            Math.Max(_selectionAnchor, newPosition)
        );
    }
}