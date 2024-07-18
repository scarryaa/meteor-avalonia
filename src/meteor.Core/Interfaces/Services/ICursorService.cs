namespace meteor.Core.Interfaces.Services;

public interface ICursorService
{
    void MoveCursor(int x, int y);
    void SetCursorPosition(int index);
    int GetCursorPosition();
}