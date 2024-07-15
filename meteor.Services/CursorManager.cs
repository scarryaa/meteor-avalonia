using meteor.Core.Interfaces;

namespace meteor.Services;

public class CursorManager(
    ITextBuffer textBuffer,
    ISelectionHandler selectionHandler,
    IWordBoundaryService wordBoundaryService)
    : ICursorManager
{
    private int _desiredColumn = -1;

    public int Position { get; private set; }

    public void SetPosition(int position)
    {
        Position = Math.Max(0, Math.Min(position, textBuffer.Length));
        UpdateDesiredColumn();
    }

    public void MoveCursorLeft(bool isShiftPressed)
    {
        if (selectionHandler.HasSelection && !isShiftPressed)
        {
            SetPosition(selectionHandler.SelectionStart);
            selectionHandler.ClearSelection();
            return;
        }

        if (Position > 0)
        {
            SetPosition(Position - 1);
            UpdateSelection(isShiftPressed);
        }
    }

    public void MoveCursorRight(bool isShiftPressed)
    {
        if (selectionHandler.HasSelection && !isShiftPressed)
        {
            SetPosition(selectionHandler.SelectionEnd);
            selectionHandler.ClearSelection();
            return;
        }

        if (Position < textBuffer.Length)
        {
            SetPosition(Position + 1);
            UpdateSelection(isShiftPressed);
        }
    }

    public void MoveCursorUp(bool isShiftPressed)
    {
        var currentLineIndex = textBuffer.GetLineIndexFromPosition(Position);
        if (currentLineIndex > 0)
        {
            var currentLineStart = textBuffer.GetLineStartPosition(currentLineIndex);
            var currentColumn = Position - currentLineStart;

            _desiredColumn = Math.Max(_desiredColumn, currentColumn);

            var previousLineIndex = currentLineIndex - 1;
            var previousLineStart = textBuffer.GetLineStartPosition(previousLineIndex);
            var previousLineLength = textBuffer.GetLineLength(previousLineIndex);

            SetPosition(previousLineStart + Math.Min(_desiredColumn, previousLineLength));
            UpdateSelection(isShiftPressed);
        }
    }

    public void MoveCursorDown(bool isShiftPressed)
    {
        var currentLineIndex = textBuffer.GetLineIndexFromPosition(Position);
        if (currentLineIndex < textBuffer.LineCount - 1)
        {
            var currentLineStart = textBuffer.GetLineStartPosition(currentLineIndex);
            var currentColumn = Position - currentLineStart;

            _desiredColumn = Math.Max(_desiredColumn, currentColumn);

            var nextLineIndex = currentLineIndex + 1;
            var nextLineStart = textBuffer.GetLineStartPosition(nextLineIndex);
            var nextLineLength = textBuffer.GetLineLength(nextLineIndex);

            SetPosition(nextLineStart + Math.Min(_desiredColumn, nextLineLength));
            UpdateSelection(isShiftPressed);
        }
    }

    public void MoveCursorToLineStart(bool isShiftPressed)
    {
        var currentLineIndex = textBuffer.GetLineIndexFromPosition(Position);
        var lineStartPosition = textBuffer.GetLineStartPosition(currentLineIndex);
        SetPosition(lineStartPosition);
        _desiredColumn = 0;
        UpdateSelection(isShiftPressed);
    }

    public void MoveCursorToLineEnd(bool isShiftPressed)
    {
        var currentLineIndex = textBuffer.GetLineIndexFromPosition(Position);
        var lineEndPosition = textBuffer.GetLineEndPosition(currentLineIndex);
        SetPosition(lineEndPosition);
        UpdateSelection(isShiftPressed);
    }

    public void MoveWordLeft(bool isShiftPressed)
    {
        var newPosition = wordBoundaryService.GetPreviousWordBoundary(textBuffer, Position);
        SetPosition(newPosition);
        UpdateSelection(isShiftPressed);
    }

    public void MoveWordRight(bool isShiftPressed)
    {
        var newPosition = wordBoundaryService.GetNextWordBoundary(textBuffer, Position);
        SetPosition(newPosition);
        UpdateSelection(isShiftPressed);
    }

    public void MoveToDocumentStart(bool isShiftPressed)
    {
        SetPosition(0);
        UpdateSelection(isShiftPressed);
    }

    public void MoveToDocumentEnd(bool isShiftPressed)
    {
        SetPosition(textBuffer.Length);
        UpdateSelection(isShiftPressed);
    }

    private void UpdateDesiredColumn()
    {
        var lineIndex = textBuffer.GetLineIndexFromPosition(Position);
        var lineStart = textBuffer.GetLineStartPosition(lineIndex);
        _desiredColumn = Position - lineStart;
    }

    private void UpdateSelection(bool isShiftPressed)
    {
        if (isShiftPressed)
        {
            if (!selectionHandler.IsSelecting) selectionHandler.StartSelection(Position);
            selectionHandler.UpdateSelectionDuringDrag(Position, false, false);
        }
        else
        {
            selectionHandler.ClearSelection();
        }
    }
}