using meteor.Core.Interfaces;

namespace meteor.Services;

public class SelectionHandler : ISelectionHandler
{
    private int _selectionEnd;
    private readonly ITextBuffer _textBuffer;
    private readonly IWordBoundaryService _wordBoundaryService;

    public SelectionHandler(ITextBuffer textBuffer, IWordBoundaryService wordBoundaryService)
    {
        _textBuffer = textBuffer;
        _wordBoundaryService = wordBoundaryService;
        ClearSelection();
    }

    public bool HasSelection => SelectionAnchor != _selectionEnd;
    public int SelectionAnchor { get; private set; }
    public int SelectionStart => Math.Min(SelectionAnchor, _selectionEnd);
    public int SelectionEnd => Math.Max(SelectionAnchor, _selectionEnd);
    public bool IsSelecting { get; private set; }

    public void StartSelection(int position)
    {
        SelectionAnchor = _selectionEnd = position;
        IsSelecting = true;
    }

    public void UpdateSelectionDuringDrag(int position, bool isDoubleClick, bool isTripleClick)
    {
        if (isTripleClick)
        {
            var lineIndex = _textBuffer.GetLineIndexFromPosition(position);
            _selectionEnd = _textBuffer.GetLineEndPosition(lineIndex);
        }
        else if (isDoubleClick)
        {
            var (_, end) = _wordBoundaryService.GetWordBoundaries(_textBuffer, position);
            _selectionEnd = end;
        }
        else
        {
            _selectionEnd = position;
        }
    }

    public void EndSelection()
    {
        IsSelecting = false;
    }

    public void ClearSelection()
    {
        SelectionAnchor = _selectionEnd = -1;
        IsSelecting = false;
    }

    public void SelectAll()
    {
        SelectionAnchor = 0;
        _selectionEnd = _textBuffer.Length;
    }

    public void SelectWord(int position)
    {
        var (start, end) = _wordBoundaryService.GetWordBoundaries(_textBuffer, position);
        SelectionAnchor = start;
        _selectionEnd = end;
    }

    public void SelectLine(int position)
    {
        var lineIndex = _textBuffer.GetLineIndexFromPosition(position);
        SelectionAnchor = _textBuffer.GetLineStartPosition(lineIndex);
        _selectionEnd = _textBuffer.GetLineEndPosition(lineIndex);
    }
}