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

    public void UpdateLineCache(long changedLineIndex, int linesInserted = 0)
    {
        _viewModel.TextBuffer.UpdateLineCache();
        _viewModel.LineCache.InvalidateLine(changedLineIndex);

        // Invalidate subsequent lines if needed
        for (var i = changedLineIndex + 1; i < _viewModel.TextBuffer.LineCount; i++)
            _viewModel.LineCache.InvalidateLine(i);

        _viewModel.NotifyGutterOfLineChange();
    }

    public List<string> GetVisibleLines()
    {
        var visibleLines = new List<string>();
        var firstVisibleLine = (int)(_viewModel.GetParentViewModel<ScrollableTextEditorViewModel>().VerticalOffset /
                                     _viewModel.LineHeight);
        var lastVisibleLine = firstVisibleLine +
                              (int)(_viewModel.GetParentViewModel<ScrollableTextEditorViewModel>().Viewport.Height /
                                    _viewModel.LineHeight) + 1;

        for (var i = firstVisibleLine; i <= lastVisibleLine && i < _viewModel.TextBuffer.LineCount; i++)
            visibleLines.Add(_viewModel.TextBuffer.GetLineText(i));

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
        for (var i = 0; i < _viewModel.TextBuffer.LineCount; i++) _viewModel.LineCache.InvalidateLine(i);
        _viewModel.OnInvalidateRequired();
    }
}