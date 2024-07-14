using System;
using System.Collections.Generic;
using meteor.ViewModels;

namespace meteor.Views.Services;

public class LineManager
{
    private TextEditorViewModel _viewModel;

    public void UpdateViewModel(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void UpdateLineCache(long changedLineIndex, long linesAffected, bool isInsertion)
    {
        var startLine = changedLineIndex;
        var endLine = changedLineIndex + Math.Abs(linesAffected);

        // Invalidate the directly affected lines
        _viewModel.LineCache.InvalidateRange(startLine, endLine);

        // If it's a deletion, we need to shift the subsequent lines
        if (!isInsertion && linesAffected < 0) ShiftLinesAfterDeletion(startLine, endLine, -linesAffected);

        _viewModel.TextBuffer.UpdateLineCache();
        _viewModel.NotifyGutterOfLineChange();
    }

    private void ShiftLinesAfterDeletion(long startLine, long endLine, long linesToShift)
    {
        for (var i = startLine; i < endLine; i++)
        {
            var lineStart = _viewModel.TextBuffer.GetLineStartPosition((int)i);
            _viewModel.TextBuffer.SetLineStartPosition((int)i, lineStart - linesToShift);
            _viewModel.LineCache.InvalidateLine(i);
        }

        for (var i = endLine; i < _viewModel.TextBuffer.LineCount; i++)
        {
            var lineStart = _viewModel.TextBuffer.GetLineStartPosition((int)i);
            _viewModel.TextBuffer.SetLineStartPosition((int)i, lineStart - linesToShift);
            _viewModel.LineCache.InvalidateLine(i);
        }
    }

    public List<string> GetVisibleLines()
    {
        var visibleLines = new List<string>();
        var scrollableViewModel = _viewModel.GetParentViewModel<ScrollableTextEditorViewModel>();
        if (scrollableViewModel == null) return visibleLines;

        var firstVisibleLine = (int)(scrollableViewModel.VerticalOffset / _viewModel.LineHeight);
        var lastVisibleLine = firstVisibleLine + (int)(scrollableViewModel.Viewport.Height / _viewModel.LineHeight) + 1;

        for (var i = firstVisibleLine; i <= lastVisibleLine && i < _viewModel.TextBuffer.LineCount; i++)
            visibleLines.Add(_viewModel.LineCache.GetLine(i,
                lineIndex => _viewModel.TextBuffer.GetLineText(lineIndex)));

        return visibleLines;
    }

    public long GetLineStartPosition(long lineIndex)
    {
        return _viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
    }

    public long GetLineEndPosition(long lineIndex)
    {
        return _viewModel.TextBuffer.GetLineEndPosition((int)lineIndex);
    }

    public long GetLineLength(long lineIndex)
    {
        return _viewModel.TextBuffer.GetLineLength(lineIndex);
    }

    public long GetLineIndexFromPosition(long position)
    {
        return _viewModel.TextBuffer.GetLineIndexFromPosition(position);
    }

    public void InvalidateAllLines()
    {
        _viewModel.LineCache.Clear();
        _viewModel.OnInvalidateRequired();
    }
}