namespace meteor.Core.Interfaces.Services;

public interface ICursorManager
{
    int Position { get; }
    void MoveCursor(int offset);
    public (double X, double Y) GetCursorPosition(ITextMeasurer textMeasurer, string text);
    int GetCursorLine();
    int GetCursorColumn();
    void SetPosition(int position);
    event EventHandler CursorPositionChanged;
}