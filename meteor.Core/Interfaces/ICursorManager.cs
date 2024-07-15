namespace meteor.Core.Interfaces;

public interface ICursorManager
{
    int Position { get; }
    void SetPosition(int position);
    void MoveCursorLeft(bool isShiftPressed);
    void MoveCursorRight(bool isShiftPressed);
    void MoveCursorUp(bool isShiftPressed);
    void MoveCursorDown(bool isShiftPressed);
    void MoveCursorToLineStart(bool isShiftPressed);
    void MoveCursorToLineEnd(bool isShiftPressed);
    void MoveWordLeft(bool isShiftPressed);
    void MoveWordRight(bool isShiftPressed);
    void MoveToDocumentStart(bool isShiftPressed);
    void MoveToDocumentEnd(bool isShiftPressed);
}