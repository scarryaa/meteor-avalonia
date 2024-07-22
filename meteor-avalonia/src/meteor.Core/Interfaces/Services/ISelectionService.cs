namespace meteor.Core.Interfaces.Services;

public interface ISelectionService
{
    void StartSelection(int index);
    void UpdateSelection(int index);
    void SetSelection(int start, int length);
    void ClearSelection();
    (int start, int length) GetSelection();
}