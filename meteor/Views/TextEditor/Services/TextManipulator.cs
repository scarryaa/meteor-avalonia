using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using meteor.ViewModels;

public class TextManipulator
{
    private TextEditorViewModel _viewModel;
    private readonly bool _useTabCharacter = false;
    private const int TabSize = 4;

    public void UpdateViewModel(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void InsertText(string text)
    {
        if (_viewModel == null)
        {
            Console.WriteLine("Warning: TextManipulator's view model is null. Cannot insert text.");
            return;
        }

        if (_viewModel.SelectionStart != _viewModel.SelectionEnd) DeleteSelectedText();

        _viewModel.TextBuffer.InsertText(_viewModel.CursorPosition, text);
        _viewModel.CursorPosition += text.Length;
        _viewModel.ClearSelection();
        _viewModel.OnInvalidateRequired();
    }

    public void DeleteSelectedText()
    {
        if (_viewModel == null)
        {
            Console.WriteLine("Warning: TextManipulator's view model is null. Cannot delete selected text.");
            return;
        }

        if (_viewModel.SelectionStart != _viewModel.SelectionEnd)
        {
            var start = Math.Min(_viewModel.SelectionStart, _viewModel.SelectionEnd);
            var length = Math.Abs(_viewModel.SelectionEnd - _viewModel.SelectionStart);
            _viewModel.TextBuffer.DeleteText(start, length);
            _viewModel.CursorPosition = start;
            _viewModel.ClearSelection();
            _viewModel.OnInvalidateRequired();
        }
    }

    public void HandleBackspace()
    {
        if (_viewModel.SelectionStart != -1 && _viewModel.SelectionEnd != -1 &&
            _viewModel.SelectionStart != _viewModel.SelectionEnd)
        {
            // Handle deletion of selected text
            var start = long.Min(_viewModel.SelectionStart, _viewModel.SelectionEnd);
            var end = long.Max(_viewModel.SelectionStart, _viewModel.SelectionEnd);
            var length = end - start;

            _viewModel.DeleteText(start, length);

            _viewModel.CursorPosition = start;
            _viewModel.ClearSelection();
        }
        else if (_viewModel.CursorPosition > 0)
        {
            // Handle deletion of a single character before the cursor
            _viewModel.DeleteText(_viewModel.CursorPosition - 1, 1);
            _viewModel.CursorPosition--;
        }
    }

    public void HandleDelete()
    {
        if (_viewModel.SelectionStart != -1 && _viewModel.SelectionEnd != -1 &&
            _viewModel.SelectionStart != _viewModel.SelectionEnd)
        {
            // Handle deletion of selected text
            var start = long.Min(_viewModel.SelectionStart, _viewModel.SelectionEnd);
            var end = long.Max(_viewModel.SelectionStart, _viewModel.SelectionEnd);
            var length = end - start;

            _viewModel.DeleteText(start, length);

            _viewModel.CursorPosition = start;
            _viewModel.ClearSelection();
        }
        else if (_viewModel.CursorPosition < _viewModel.TextBuffer.Length)
        {
            // Handle deletion of a single character
            var lineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(_viewModel.CursorPosition);
            _viewModel.DeleteText(_viewModel.CursorPosition, 1);
        }
    }

    public void InsertNewLine()
    {
        InsertText(Environment.NewLine);
    }

    public void InsertTab()
    {
        var tabString = _useTabCharacter ? "\t" : new string(' ', TabSize);
        _viewModel.InsertText(_viewModel.CursorPosition, tabString);
        _viewModel.ClearSelection();
    }

    public void RemoveTab()
    {
        var tabString = _useTabCharacter ? "\t" : new string(' ', TabSize);
        var lineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(_viewModel.CursorPosition);
        var lineStart = _viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineText = _viewModel.TextBuffer.GetLineText(lineIndex);
        var cursorColumn = (int)(_viewModel.CursorPosition - lineStart);

        // Check if there are tabs/spaces before the cursor position
        var deleteLength = 0;
        for (var i = cursorColumn - tabString.Length; i >= 0; i--)
            if (i >= 0 && i + tabString.Length <= lineText.Length &&
                lineText.Substring(i, tabString.Length) == tabString)
            {
                deleteLength = tabString.Length;
                _viewModel.DeleteText(lineStart + i, deleteLength);
                _viewModel.UpdateLineCache(lineIndex, tabString);

                _viewModel.CursorPosition = lineStart + i;
                break;
            }

        // If no tabs/spaces were deleted before the cursor, unindent the line
        if (deleteLength == 0 && lineText.StartsWith(tabString))
        {
            _viewModel.DeleteText(lineStart, tabString.Length);
            _viewModel.UpdateLineCache(lineIndex, tabString);

            _viewModel.CursorPosition = Math.Max(_viewModel.CursorPosition - tabString.Length, lineStart);
        }
    }

    public void ReplaceText(long start, long length, string newText)
    {
        _viewModel.TextBuffer.DeleteText(start, length);
        _viewModel.TextBuffer.InsertText(start, newText);
        _viewModel.CursorPosition = start + newText.Length;
        _viewModel.ClearSelection();
        _viewModel.OnInvalidateRequired();
    }

    public async void IndentSelectionAsync()
    {
        await IndentOperationAsync(false);
    }

    public async void UnindentSelectionAsync()
    {
        await IndentOperationAsync(true);
    }

    private async Task IndentOperationAsync(bool isShiftTab)
    {
        var startLine =
            _viewModel.TextBuffer.GetLineIndexFromPosition(Math.Min(_viewModel.SelectionStart,
                _viewModel.SelectionEnd));
        var endLine =
            _viewModel.TextBuffer.GetLineIndexFromPosition(Math.Max(_viewModel.SelectionStart,
                _viewModel.SelectionEnd));

        var tabString = _useTabCharacter ? "\t" : new string(' ', TabSize);

        // Get modifications
        var entireTextSelected = _viewModel.SelectionManager.IsEntireTextSelected(_viewModel);
        var modifications = await Task.Run(() =>
            _viewModel.SelectionManager.PrepareModifications(_viewModel, (int)startLine, (int)endLine, tabString,
                isShiftTab));

        // Apply modifications directly on the UI thread
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _viewModel.SelectionManager.ApplyModificationsAsync(_viewModel, modifications);

            // Update selection based on modifications
            if (!entireTextSelected)
            {
                _viewModel.SelectionManager.UpdateSelectionAfterTabbing(_viewModel, startLine, endLine, isShiftTab,
                    modifications);
            }
            else if (isShiftTab)
            {
                // Ensure selection end is within the new buffer length after shift tab
                _viewModel.SelectionEnd = Math.Min(_viewModel.SelectionEnd, _viewModel.TextBuffer.Length);
                _viewModel.CursorPosition = _viewModel.SelectionEnd;
            }
            else
            {
                _viewModel.SelectionEnd = _viewModel.TextBuffer.Length;
                _viewModel.CursorPosition = _viewModel.SelectionEnd;
            }

            _viewModel.NotifyGutterOfLineChange();
        });
    }
}