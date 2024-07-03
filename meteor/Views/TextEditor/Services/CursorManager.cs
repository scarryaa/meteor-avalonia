using meteor.ViewModels;

namespace meteor.Views.Services;

public class CursorManager
{
    private TextEditorViewModel _viewModel;
    private long _desiredColumn;

    public CursorManager(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
        _desiredColumn = -1;
    }

    public void UpdateViewModel(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void MoveCursorLeft(bool isShiftPressed)
    {
        if (_viewModel.SelectionStart != _viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the start of the selection
            _viewModel.CursorPosition = long.Min(_viewModel.SelectionStart, _viewModel.SelectionEnd);
            _viewModel.ClearSelection();
            return;
        }

        if (_viewModel.CursorPosition > 0)
        {
            _viewModel.CursorPosition--;
            UpdateDesiredColumn();
            if (isShiftPressed)
                _viewModel.SelectionEnd = _viewModel.CursorPosition;
            else
                _viewModel.ClearSelection();
        }
    }

    public void MoveCursorRight(bool isShiftPressed)
    {
        if (_viewModel.SelectionStart != _viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the end of the selection
            _viewModel.CursorPosition = long.Max(_viewModel.SelectionStart, _viewModel.SelectionEnd);
            _viewModel.ClearSelection();
            return;
        }

        if (_viewModel.CursorPosition < _viewModel.TextBuffer.Length)
        {
            _viewModel.CursorPosition++;
            UpdateDesiredColumn();
            if (isShiftPressed)
                _viewModel.SelectionEnd = _viewModel.CursorPosition;
            else
                _viewModel.ClearSelection();
        }
    }

    public void MoveCursorUp(bool isShiftPressed)
    {
        if (_viewModel.SelectionStart != _viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the start of the selection
            _viewModel.CursorPosition = long.Min(_viewModel.SelectionStart, _viewModel.SelectionEnd);
            _viewModel.SelectionManager.UpdateSelection();
            return;
        }

        var currentLineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(_viewModel.CursorPosition);
        if (currentLineIndex > 0)
        {
            var currentLineStart = _viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
            var currentColumn = _viewModel.CursorPosition - currentLineStart;

            // Update desired column only if it's greater than the current column
            _desiredColumn = long.Max(_desiredColumn, currentColumn);

            var previousLineIndex = currentLineIndex - 1;
            var previousLineStart = _viewModel.TextBuffer.GetLineStartPosition((int)previousLineIndex);
            var previousLineLength = _viewModel.TextBuffer.GetLineLength(previousLineIndex);

            // Calculate new cursor position
            _viewModel.CursorPosition = previousLineStart + long.Min(_desiredColumn, previousLineLength - 1);
        }
        else
        {
            // Move to the start of the first line
            _viewModel.CursorPosition = 0;
            UpdateDesiredColumn();
        }

        if (isShiftPressed)
            // Update selection
            _viewModel.SelectionManager.UpdateSelection(end: _viewModel.CursorPosition);
        else
            _viewModel.ClearSelection();
    }

    public void MoveCursorDown(bool isShiftPressed)
    {
        if (_viewModel.SelectionStart != _viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the end of the selection
            _viewModel.CursorPosition = long.Max(_viewModel.SelectionStart, _viewModel.SelectionEnd);
            _viewModel.ClearSelection();
            return;
        }

        var currentLineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(_viewModel.CursorPosition);
        if (currentLineIndex < _viewModel.TextBuffer.LineCount - 1)
        {
            var currentLineStart = _viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
            var currentColumn = _viewModel.CursorPosition - currentLineStart;

            // Update the desired column only if it's greater than the current column
            _desiredColumn = long.Max(_desiredColumn, currentColumn);

            var nextLineIndex = currentLineIndex + 1;
            var nextLineStart = _viewModel.TextBuffer.GetLineStartPosition((int)nextLineIndex);
            var nextLineLength = _viewModel.TextBuffer.GetVisualLineLength((int)nextLineIndex);

            // Calculate new cursor position
            _viewModel.CursorPosition = nextLineStart + long.Min(_desiredColumn, nextLineLength);
        }
        else
        {
            // If the document is empty or at the end of the last line, set cursor to the end of the document
            if (_viewModel.TextBuffer.Length == 0)
            {
                _viewModel.CursorPosition = 0;
            }
            else
            {
                var lastLineStart = _viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
                var lastLineLength = _viewModel.TextBuffer.GetLineLength((int)currentLineIndex);
                _viewModel.CursorPosition = lastLineStart + lastLineLength;
            }

            UpdateDesiredColumn();
        }

        if (isShiftPressed)
            // Update selection
            _viewModel.SelectionEnd = _viewModel.CursorPosition;
        else
            _viewModel.ClearSelection();
    }

    public void MoveCursorToLineStart(bool isShiftPressed)
    {
        var currentLineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(_viewModel.CursorPosition);
        var lineStartPosition = _viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
        _viewModel.CursorPosition = lineStartPosition;
        _desiredColumn = 0;
        if (!isShiftPressed) _viewModel.ClearSelection();
    }

    public void MoveCursorToLineEnd(bool isShiftPressed)
    {
        var currentLineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(_viewModel.CursorPosition);
        var lineStartPosition = _viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
        var lineLength = _viewModel.TextBuffer.GetVisualLineLength((int)currentLineIndex);
        _viewModel.CursorPosition = lineStartPosition + lineLength;
        UpdateDesiredColumn();
        if (!isShiftPressed) _viewModel.ClearSelection();
    }

    public void MoveCursorToPreviousWord(TextEditorViewModel viewModel, bool extendSelection)
    {
        if (viewModel.CursorPosition == 0) return;

        var lineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.CursorPosition);
        var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);

        if (viewModel.CursorPosition == lineStart)
        {
            if (lineIndex > 0) viewModel.CursorPosition = viewModel.TextBuffer.GetLineEndPosition((int)(lineIndex - 1));
            return;
        }

        var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
        var index = (int)(viewModel.CursorPosition - lineStart - 1);

        while (index > 0 && char.IsWhiteSpace(lineText[index])) index--;

        if (index > 0)
        {
            if (_viewModel.TextEditorUtils.IsCommonCodingSymbol(lineText[index]))
                while (index > 0 && _viewModel.TextEditorUtils.IsCommonCodingSymbol(lineText[index - 1]))
                    index--;
            else
                while (index > 0 && !char.IsWhiteSpace(lineText[index - 1]) &&
                       !_viewModel.TextEditorUtils.IsCommonCodingSymbol(lineText[index - 1]))
                    index--;
        }

        viewModel.CursorPosition = lineStart + index;
        _viewModel.SelectionManager.UpdateSelectionAfterCursorMove(_viewModel, extendSelection);
    }

    public void MoveCursorToNextWord(TextEditorViewModel viewModel, bool extendSelection)
    {
        var lineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.CursorPosition);
        var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineEnd = viewModel.TextBuffer.GetLineEndPosition((int)lineIndex);

        if (viewModel.CursorPosition >= lineEnd)
        {
            if (lineIndex < viewModel.TextBuffer.LineCount - 1)
                viewModel.CursorPosition = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex + 1);
            return;
        }

        var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
        var index = (int)(viewModel.CursorPosition - lineStart);

        while (index < lineText.Length && char.IsWhiteSpace(lineText[index])) index++;

        if (index < lineText.Length)
        {
            if (_viewModel.TextEditorUtils.IsCommonCodingSymbol(lineText[index]))
                while (index < lineText.Length && _viewModel.TextEditorUtils.IsCommonCodingSymbol(lineText[index]))
                    index++;
            else
                while (index < lineText.Length && !char.IsWhiteSpace(lineText[index]) &&
                       !_viewModel.TextEditorUtils.IsCommonCodingSymbol(lineText[index]))
                    index++;
        }

        viewModel.CursorPosition = lineStart + index;
        _viewModel.SelectionManager.UpdateSelectionAfterCursorMove(_viewModel, extendSelection);
    }

    private void UpdateDesiredColumn()
    {
        var lineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(_viewModel.CursorPosition);

        if (lineIndex >= _viewModel.TextBuffer.LineStarts.Count) _viewModel.TextBuffer.UpdateLineCache();

        if (lineIndex >= 0 && lineIndex < _viewModel.TextBuffer.LineStarts.Count)
        {
            var lineStart = _viewModel.TextBuffer.LineStarts[(int)lineIndex];
            _desiredColumn = _viewModel.CursorPosition - lineStart;
        }
        else
        {
            _desiredColumn = 0;
        }
    }
}