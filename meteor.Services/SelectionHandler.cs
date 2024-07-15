using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Events;
using meteor.Core.Models.Events;

namespace meteor.Services;

public class SelectionHandler : ISelectionHandler
{
    private int _selectionEnd;
    private readonly ITextBuffer _textBuffer;
    private readonly IWordBoundaryService _wordBoundaryService;
    private readonly IEventAggregator _eventAggregator;

    public SelectionHandler(ITextBuffer textBuffer, IWordBoundaryService wordBoundaryService,
        IEventAggregator eventAggregator)
    {
        _textBuffer = textBuffer;
        _wordBoundaryService = wordBoundaryService;
        _eventAggregator = eventAggregator;
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
        PublishSelectionChanged();
    }

    public void UpdateSelection(int position)
    {
        _selectionEnd = position;
        PublishSelectionChanged();
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
            var (start, end) = _wordBoundaryService.GetWordBoundaries(_textBuffer, position);
            _selectionEnd = SelectionAnchor < position ? end : start;
        }
        else
        {
            _selectionEnd = position;
        }

        PublishSelectionChanged();
    }

    public void EndSelection()
    {
        IsSelecting = false;
        PublishSelectionChanged();
    }

    public void ClearSelection()
    {
        SelectionAnchor = _selectionEnd = -1;
        IsSelecting = false;
        PublishSelectionChanged();
    }

    public void SelectAll()
    {
        SelectionAnchor = 0;
        _selectionEnd = _textBuffer.Length;
        PublishSelectionChanged();
    }

    public void SelectWord(int position)
    {
        var (start, end) = _wordBoundaryService.GetWordBoundaries(_textBuffer, position);
        SelectionAnchor = start;
        _selectionEnd = end;
        PublishSelectionChanged();
    }

    public void SelectLine(int position)
    {
        var lineIndex = _textBuffer.GetLineIndexFromPosition(position);
        SelectionAnchor = _textBuffer.GetLineStartPosition(lineIndex);
        _selectionEnd = _textBuffer.GetLineEndPosition(lineIndex);
        PublishSelectionChanged();
    }

    private void PublishSelectionChanged()
    {
        _eventAggregator.Publish(new SelectionChangedEventArgs(SelectionStart, SelectionEnd, IsSelecting));
    }
}
