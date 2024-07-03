using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using meteor.Models;
using meteor.ViewModels;

namespace meteor.Views.Services;

public class SelectionManager
{
    private TextEditorViewModel _viewModel;

    public long SelectionAnchor { get; set; }

    public SelectionManager(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void UpdateViewModel(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void StartSelection()
    {
        SelectionAnchor = _viewModel.CursorPosition;
        _viewModel.SelectionStart = _viewModel.CursorPosition;
        _viewModel.SelectionEnd = _viewModel.CursorPosition;
    }

    public void UpdateSelectionAfterCursorMove(TextEditorViewModel viewModel, bool extendSelection)
    {
        if (extendSelection)
        {
            viewModel.SelectionEnd = viewModel.CursorPosition;
            UpdateSelection();
        }
        else
        {
            viewModel.ClearSelection();
        }
    }

    public void SetSelection(long start, long end)
    {
        SelectionAnchor = start;
        _viewModel.SelectionStart = start;
        _viewModel.SelectionEnd = end;
        _viewModel.CursorPosition = end;
    }

    public void UpdateSelection(long? start = null, long? end = null)
    {
        var selectionStart = start ?? _viewModel.CursorPosition;
        var selectionEnd = end ?? SelectionAnchor;

        if (selectionStart < selectionEnd)
        {
            _viewModel.SelectionStart = selectionStart;
            _viewModel.SelectionEnd = selectionEnd;
        }
        else
        {
            _viewModel.SelectionStart = selectionEnd;
            _viewModel.SelectionEnd = selectionStart;
        }

        if (start.HasValue && end.HasValue)
        {
            SelectionAnchor = start.Value;
            _viewModel.CursorPosition = end.Value;
        }
    }

    public void SelectAll()
    {
        SelectionAnchor = 0;
        _viewModel.SelectionStart = 0;
        _viewModel.SelectionEnd = _viewModel.TextBuffer.Length;

        _viewModel.ShouldScrollToCursor = false;
        _viewModel.CursorPosition = _viewModel.TextBuffer.Length;
        _viewModel.ShouldScrollToCursor = true;
    }

    public void SelectWord()
    {
        var (wordStart, wordEnd) =
            _viewModel.TextEditorUtils.FindWordOrSymbolBoundaries(_viewModel, _viewModel.CursorPosition);
        SetSelection(wordStart, wordEnd);
    }

    public void SelectLine()
    {
        var lineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(_viewModel.CursorPosition);
        var lineStart = _viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineEnd = _viewModel.TextBuffer.GetLineEndPosition((int)lineIndex);

        SetSelection(lineStart, lineEnd);
    }

    public void UpdateSelectionAfterTabbing(TextEditorViewModel viewModel, long startLine, long endLine,
        bool isShiftTab, List<(long position, int deleteLength, string insertText)> modifications)
    {
        if (isShiftTab)
        {
            var anyTabsRemoved = modifications.Any(m => m.deleteLength > 0);

            if (anyTabsRemoved)
            {
                var lineStartPos = viewModel.TextBuffer.GetLineStartPosition((int)startLine);
                var totalShift = modifications.Sum(m => m.deleteLength);
                viewModel.SelectionStart = Math.Max(viewModel.SelectionStart - totalShift, lineStartPos);

                var selectionEndOffset = 0;
                var lastLineIndex = -1.0;
                for (var lineIndex = startLine; lineIndex <= endLine; lineIndex++)
                {
                    var modificationsOnLine = modifications
                        .Where(m => _viewModel.TextBuffer.GetLineIndexFromPosition(m.position) == lineIndex)
                        .Sum(m => m.deleteLength);
                    selectionEndOffset += modificationsOnLine;

                    lastLineIndex = lineIndex;
                }

                if (lastLineIndex < 0 || lastLineIndex >= viewModel.TextBuffer.LineCount)
                {
                    Console.WriteLine(
                        $"Invalid lastLineIndex: {lastLineIndex}, LineCount: {viewModel.TextBuffer.LineCount}");
                    lastLineIndex = viewModel.TextBuffer.LineCount - 1;
                }

                var lastLineEndPos = viewModel.TextBuffer.GetLineEndPosition((int)lastLineIndex);
                viewModel.SelectionEnd = Math.Min(viewModel.SelectionEnd - selectionEndOffset, lastLineEndPos);
                viewModel.SelectionEnd = Math.Min(viewModel.SelectionEnd, viewModel.TextBuffer.Length);

                // Adjust cursor position based on deletions
                var cursorShift = modifications.Where(m => m.position <= viewModel.CursorPosition)
                    .Sum(m => m.deleteLength);
                viewModel.CursorPosition = Math.Max(viewModel.CursorPosition - cursorShift, lineStartPos);
            }
        }
        else
        {
            var totalShift = modifications.Sum(m => m.insertText.Length);
            viewModel.SelectionStart = Math.Min(viewModel.SelectionStart, viewModel.TextBuffer.Length + 1);
            viewModel.SelectionEnd = Math.Min(viewModel.SelectionEnd + totalShift, viewModel.TextBuffer.Length);

            // Adjust cursor position based on insertions
            var cursorShift = modifications.Where(m => m.position <= viewModel.CursorPosition)
                .Sum(m => m.insertText.Length);
            viewModel.SelectionEnd = Math.Min(viewModel.SelectionEnd, viewModel.TextBuffer.Length);
            viewModel.CursorPosition += cursorShift;
        }
    }

    public void UpdateSelectionDuringManualScroll(long position)
    {
        if (_viewModel.InputManager.IsTripleClickDrag)
            UpdateTripleClickSelection(_viewModel, position);
        else if (_viewModel.InputManager.IsDoubleClickDrag)
            UpdateDoubleClickSelection(_viewModel, position);
        else
            _viewModel.SelectionManager.UpdateNormalSelection(_viewModel, position);
    }

    private void UpdateNormalSelection(TextEditorViewModel viewModel, long position)
    {
        viewModel.CursorPosition = position;
        if (position < SelectionAnchor)
        {
            viewModel.SelectionStart = position;
            viewModel.SelectionEnd = SelectionAnchor;
        }
        else
        {
            viewModel.SelectionStart = SelectionAnchor;
            viewModel.SelectionEnd = position;
        }
    }

    public void UpdateDoubleClickSelection(TextEditorViewModel viewModel, long position)
    {
        var (currentWordStart, currentWordEnd) =
            _viewModel.TextEditorUtils.FindWordOrSymbolBoundaries(viewModel, position);
        var (anchorWordStart, anchorWordEnd) =
            _viewModel.TextEditorUtils.FindWordOrSymbolBoundaries(viewModel, SelectionAnchor);

        if (position < SelectionAnchor)
        {
            viewModel.SelectionStart = Math.Min(currentWordStart, anchorWordStart);
            viewModel.SelectionEnd = Math.Max(anchorWordEnd, SelectionAnchor);
            viewModel.CursorPosition = currentWordStart;
        }
        else
        {
            viewModel.SelectionStart = Math.Min(anchorWordStart, SelectionAnchor);
            viewModel.SelectionEnd = Math.Max(currentWordEnd, anchorWordEnd);
            viewModel.CursorPosition = currentWordEnd;
        }
    }

    public void UpdateTripleClickSelection(TextEditorViewModel viewModel, long position)
    {
        var currentLineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(position);
        var anchorLineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(SelectionAnchor);

        var currentLineStart = viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
        var currentLineEnd = currentLineStart + viewModel.TextBuffer.GetVisualLineLength((int)currentLineIndex);

        var anchorLineStart = viewModel.TextBuffer.GetLineStartPosition((int)anchorLineIndex);
        var anchorLineEnd = anchorLineStart + viewModel.TextBuffer.GetVisualLineLength((int)anchorLineIndex);

        if (currentLineIndex < anchorLineIndex)
        {
            viewModel.SelectionStart = currentLineStart;
            viewModel.SelectionEnd = anchorLineEnd;
            viewModel.CursorPosition = currentLineStart;
        }
        else
        {
            viewModel.SelectionStart = anchorLineStart;
            viewModel.SelectionEnd = currentLineEnd;
            viewModel.CursorPosition = currentLineEnd;
        }
    }

    public void SelectTrailingWhitespace(TextEditorViewModel viewModel, long lineIndex, string lineText,
        long lineStart)
    {
        var lastNonWhitespaceIndex = lineText.Length - 1;
        while (lastNonWhitespaceIndex >= 0 && char.IsWhiteSpace(lineText[lastNonWhitespaceIndex]))
            lastNonWhitespaceIndex--;

        var trailingWhitespaceStart = lineStart + lastNonWhitespaceIndex + 1;
        var trailingWhitespaceEnd = lineStart + _viewModel.TextBuffer.GetVisualLineLength((int)lineIndex);

        // If there's no trailing whitespace, select the word or symbol at the end of the line
        if (trailingWhitespaceStart == trailingWhitespaceEnd && lastNonWhitespaceIndex >= 0)
        {
            // Find the boundaries of the word or symbol at the end of the line
            var (wordStart, wordEnd) =
                _viewModel.TextEditorUtils.FindWordOrSymbolBoundaries(viewModel, lineStart + lastNonWhitespaceIndex);
            trailingWhitespaceStart = wordStart;
            trailingWhitespaceEnd = wordEnd;
        }

        viewModel.SelectionStart = trailingWhitespaceStart;
        viewModel.SelectionEnd = trailingWhitespaceEnd;
        viewModel.CursorPosition = trailingWhitespaceEnd;
        SelectionAnchor = trailingWhitespaceStart;

        UpdateSelection();
    }

    public List<(long position, int deleteLength, string insertText)> PrepareModifications(
        TextEditorViewModel viewModel, int startLine, int endLine, string tabString, bool isShiftTab)
    {
        var modifications = new List<(long position, int deleteLength, string insertText)>();

        for (var lineIndex = startLine; lineIndex <= endLine; lineIndex++)
        {
            if (lineIndex < 0 || lineIndex >= viewModel.TextBuffer.LineCount)
            {
                Console.WriteLine(
                    $"Invalid lineIndex: {lineIndex}, LineStarts.Count: {viewModel.TextBuffer.LineStarts.Count}");
                continue;
            }

            var lineStart = viewModel.TextBuffer.GetLineStartPosition(lineIndex);
            var lineText = viewModel.TextBuffer.GetLineText(lineIndex);

            if (isShiftTab)
            {
                var positionToDelete = lineText.StartsWith(tabString) ? 0 : -1;
                if (positionToDelete >= 0)
                    modifications.Add((lineStart + positionToDelete, tabString.Length, string.Empty));
            }
            else
            {
                modifications.Add((lineStart, 0, tabString));
            }
        }

        return modifications;
    }

    public async Task<int> ApplyModificationsAsync(TextEditorViewModel viewModel,
        List<(long position, int deleteLength, string insertText)> modifications)
    {
        var actualTabLength = 0;
        long offset = 0;
        var sb = new StringBuilder(viewModel.TextBuffer.Text);

        foreach (var (position, originalDeleteLength, insertText) in modifications)
        {
            var deleteLength = originalDeleteLength;

            if (position + offset < 0 || position + offset >= sb.Length)
            {
                Console.WriteLine($"Position {position + offset} is out of bounds. Skipping modification.");
                continue;
            }

            if (deleteLength > 0)
            {
                if (position + offset + deleteLength > sb.Length)
                {
                    Console.WriteLine(
                        $"Delete length {deleteLength} from position {position + offset} exceeds string bounds. Adjusting length.");
                    deleteLength = sb.Length - (int)(position + offset);
                }

                sb.Remove((int)(position + offset), deleteLength);
                offset -= deleteLength;
                actualTabLength = deleteLength;
            }

            if (!string.IsNullOrEmpty(insertText))
            {
                sb.Insert((int)(position + offset), insertText);
                offset += insertText.Length;
                actualTabLength = insertText.Length;
            }
        }

        // Final update of text buffer and line starts
        viewModel.TextBuffer.SetText(sb.ToString());

        return actualTabLength;
    }

    public bool IsEntireTextSelected(TextEditorViewModel viewModel)
    {
        return viewModel.SelectionStart == 0 && viewModel.SelectionEnd == viewModel.TextBuffer.Length;
    }

    private bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    public void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.OnInvalidateRequired();
    }
}