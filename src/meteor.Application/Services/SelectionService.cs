using meteor.Core.Interfaces.Services;

namespace meteor.Application.Services;

public class SelectionService : ISelectionService
{
    private int _selectionEnd = -1;
    private int _selectionStart = -1;

    public void StartSelection(int index)
    {
        _selectionStart = index;
        _selectionEnd = index;
    }

    public void UpdateSelection(int index)
    {
        _selectionEnd = index;
    }

    public void SetSelection(int start, int length)
    {
        _selectionStart = start;
        _selectionEnd = start + length;
    }

    public void ClearSelection()
    {
        _selectionStart = -1;
        _selectionEnd = -1;
    }

    public (int start, int length) GetSelection()
    {
        if (_selectionStart == -1 || _selectionEnd == -1) return (-1, 0);
        var start = Math.Min(_selectionStart, _selectionEnd);
        var end = Math.Max(_selectionStart, _selectionEnd);
        return (start, end - start);
    }
}