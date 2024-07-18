using meteor.Core.Interfaces.Services;

namespace meteor.Application.Services;

public class SelectionService : ISelectionService
{
    private int _selectionStart;
    private int _selectionLength;

    public void StartSelection(int index)
    {
        _selectionStart = index;
        _selectionLength = 0;
    }

    public void UpdateSelection(int index)
    {
        _selectionLength = index - _selectionStart;
    }

    public void SetSelection(int start, int length)
    {
        _selectionStart = start;
        _selectionLength = length;
    }

    public void ClearSelection()
    {
        _selectionStart = 0;
        _selectionLength = 0;
    }

    public (int start, int length) GetSelection()
    {
        return (_selectionStart, _selectionLength);
    }
}