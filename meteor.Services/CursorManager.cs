using meteor.Core.Interfaces;

namespace meteor.Services;

public class CursorManager : ICursorManager
{
    private readonly ITextBuffer _textBuffer;
    private readonly ISelectionHandler _selectionHandler;
    private readonly IWordBoundaryService _wordBoundaryService;
    private int _desiredColumn;

    public CursorManager(ITextBuffer textBuffer, ISelectionHandler selectionHandler,
        IWordBoundaryService wordBoundaryService)
    {
        _textBuffer = textBuffer;
        _selectionHandler = selectionHandler;
        _wordBoundaryService = wordBoundaryService;
        _desiredColumn = -1;
        Position = 0;
    }

    public int Position { get; private set; }

    public void SetPosition(int position)
    {
        Position = Math.Max(0, Math.Min(position, _textBuffer.Length));
        UpdateDesiredColumn();
    }

    public void MoveCursorLeft(bool isShiftPressed)
    {
        if (_selectionHandler.HasSelection && !isShiftPressed)
        {
            SetPosition(_selectionHandler.SelectionStart);
            _selectionHandler.ClearSelection();
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
        if (_selectionHandler.HasSelection && !isShiftPressed)
        {
            SetPosition(_selectionHandler.SelectionEnd);
            _selectionHandler.ClearSelection();
            return;
        }

        if (Position < _textBuffer.Length)
        {
            SetPosition(Position + 1);
            UpdateSelection(isShiftPressed);
        }
    }

    public void MoveCursorUp(bool isShiftPressed)
    {
        var currentLineIndex = _textBuffer.GetLineIndexFromPosition(Position);
        if (currentLineIndex > 0)
        {
            var currentLineStart = _textBuffer.GetLineStartPosition(currentLineIndex);
            var currentColumn = Position - currentLineStart;

            _desiredColumn = Math.Max(_desiredColumn, currentColumn);

            var previousLineIndex = currentLineIndex - 1;
            var previousLineStart = _textBuffer.GetLineStartPosition(previousLineIndex);
            var previousLineLength = _textBuffer.GetLineLength(previousLineIndex);

            SetPosition(previousLineStart + Math.Min(_desiredColumn, previousLineLength));
            UpdateSelection(isShiftPressed);
        }
    }

    public void MoveCursorDown(bool isShiftPressed)
    {
        var currentLineIndex = _textBuffer.GetLineIndexFromPosition(Position);
        if (currentLineIndex < _textBuffer.LineCount - 1)
        {
            var currentLineStart = _textBuffer.GetLineStartPosition(currentLineIndex);
            var currentColumn = Position - currentLineStart;

            _desiredColumn = Math.Max(_desiredColumn, currentColumn);

            var nextLineIndex = currentLineIndex + 1;
            var nextLineStart = _textBuffer.GetLineStartPosition(nextLineIndex);
            var nextLineLength = _textBuffer.GetLineLength(nextLineIndex);

            SetPosition(nextLineStart + Math.Min(_desiredColumn, nextLineLength));
            UpdateSelection(isShiftPressed);
        }
    }

    public void MoveCursorToLineStart(bool isShiftPressed)
    {
        var currentLineIndex = _textBuffer.GetLineIndexFromPosition(Position);
        var lineStartPosition = _textBuffer.GetLineStartPosition(currentLineIndex);
        SetPosition(lineStartPosition);
        _desiredColumn = 0;
        UpdateSelection(isShiftPressed);
    }

    public void MoveCursorToLineEnd(bool isShiftPressed)
    {
        var currentLineIndex = _textBuffer.GetLineIndexFromPosition(Position);
        var lineEndPosition = _textBuffer.GetLineEndPosition(currentLineIndex);
        SetPosition(lineEndPosition);
        UpdateSelection(isShiftPressed);
    }

    public void MoveWordLeft(bool isShiftPressed)
    {
        var newPosition = _wordBoundaryService.GetPreviousWordBoundary(_textBuffer, Position);
        SetPosition(newPosition);
        UpdateSelection(isShiftPressed);
    }

    public void MoveWordRight(bool isShiftPressed)
    {
        var newPosition = _wordBoundaryService.GetNextWordBoundary(_textBuffer, Position);
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
        SetPosition(_textBuffer.Length);
        UpdateSelection(isShiftPressed);
    }

    private void UpdateDesiredColumn()
    {
        var lineIndex = _textBuffer.GetLineIndexFromPosition(Position);
        var lineStart = _textBuffer.GetLineStartPosition(lineIndex);
        _desiredColumn = Position - lineStart;
    }

    private void UpdateSelection(bool isShiftPressed)
    {
        if (isShiftPressed)
        {
            if (!_selectionHandler.IsSelecting) _selectionHandler.StartSelection(Position);
            _selectionHandler.UpdateSelectionDuringDrag(Position, false, false);
        }
        else
        {
            _selectionHandler.ClearSelection();
        }
    }
}