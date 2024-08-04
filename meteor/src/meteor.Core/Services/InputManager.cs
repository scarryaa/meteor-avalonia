using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.EventArgs;

namespace meteor.Core.Services;

public class InputManager : IInputManager
{
    private readonly IClipboardManager _clipboardManager;
    private readonly ICursorManager _cursorManager;
    private readonly IScrollManager _scrollManager;
    private readonly ISelectionManager _selectionManager;
    private readonly ITextAnalysisService _textAnalysisService;
    private readonly ITextBufferService _textBufferService;
    private bool _isAltPressed;
    private bool _isClipboardOperationHandled;
    private bool _isControlOrMetaPressed;
    private bool _isShiftPressed;
    private IEditorViewModel _viewModel;

    public InputManager(
        ITextBufferService textBufferService,
        ICursorManager cursorManager,
        IClipboardManager clipboardManager,
        ISelectionManager selectionManager,
        ITextAnalysisService textAnalysisService,
        IScrollManager scrollManager)
    {
        _textBufferService = textBufferService ?? throw new ArgumentNullException(nameof(textBufferService));
        _cursorManager = cursorManager ?? throw new ArgumentNullException(nameof(cursorManager));
        _clipboardManager = clipboardManager ?? throw new ArgumentNullException(nameof(clipboardManager));
        _selectionManager = selectionManager ?? throw new ArgumentNullException(nameof(selectionManager));
        _textAnalysisService = textAnalysisService ?? throw new ArgumentNullException(nameof(textAnalysisService));
        _scrollManager = scrollManager ?? throw new ArgumentNullException(nameof(scrollManager));
    }

    public async Task HandleKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.Modifiers == KeyModifiers.Control)
        {
            await _viewModel.TriggerCompletionAsync();
            e.Handled = true;
            return;
        }

        if (_viewModel.IsCompletionActive)
        {
            HandleCompletionKeyDown(e);
            if (e.Handled) return;

            switch (e.Key)
            {
                case Key.Escape:
                case Key.Enter:
                case Key.Tab:
                case Key.Left:
                case Key.Right:
                case Key.Home:
                case Key.End:
                    _viewModel.CloseCompletion();
                    break;
            }
        }

        try
        {
            _isClipboardOperationHandled = false;
            _isShiftPressed = e.Modifiers.HasFlag(KeyModifiers.Shift);
            _isControlOrMetaPressed =
                e.Modifiers.HasFlag(KeyModifiers.Control) || e.Modifiers.HasFlag(KeyModifiers.Meta);
            _isAltPressed = e.Modifiers.HasFlag(KeyModifiers.Alt);

            switch (e.Key)
            {
                case Key.Enter:
                    HandleEnterKey();
                    break;
                case Key.Tab:
                    if (_isShiftPressed)
                        HandleShiftTabKey();
                    else
                        HandleTabKey();
                    break;
                case Key.Left:
                    HandleLeftKey();
                    break;
                case Key.Right:
                    HandleRightKey();
                    break;
                case Key.Up:
                    if (_isControlOrMetaPressed)
                        HandleScrollUpKey();
                    else
                        HandleUpKey();
                    break;
                case Key.Down:
                    if (_isControlOrMetaPressed)
                        HandleScrollDownKey();
                    else
                        HandleDownKey();
                    break;
                case Key.Back:
                    await HandleBackspaceKey();
                    break;
                case Key.Delete:
                    await HandleDeleteKey();
                    break;
                case Key.Home:
                    if (_isControlOrMetaPressed)
                        HandleCtrlHomeKey();
                    else
                        HandleHomeKey();
                    break;
                case Key.End:
                    if (_isControlOrMetaPressed)
                        HandleCtrlEndKey();
                    else
                        HandleEndKey();
                    break;
                case Key.PageUp:
                    HandlePageUpKey();
                    break;
                case Key.PageDown:
                    HandlePageDownKey();
                    break;
                case Key.X:
                case Key.C:
                case Key.V:
                    if (_isControlOrMetaPressed)
                    {
                        await HandleClipboardOperation(e.Key);
                        _isClipboardOperationHandled = true;
                    }
                    else
                    {
                        DeleteSelectedText();
                    }
                    break;
                case Key.A:
                    if (_isControlOrMetaPressed)
                        HandleSelectAll();
                    else
                        DeleteSelectedText();
                    break;
                case Key.Z:
                    if (_isControlOrMetaPressed)
                    {
                        if (_isShiftPressed)
                            _viewModel.Redo();
                        else
                            _viewModel.Undo();
                    }
                    else
                        DeleteSelectedText();
                    break;
                case Key.Y:
                    DeleteSelectedText();
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    // Do nothing for Alt key presses
                    break;
                default:
                    return;
            }

            e.Handled = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling key down event: {ex.Message}");
        }
    }

    public async Task HandleTextInput(TextInputEventArgs e)
    {
        if (_isClipboardOperationHandled || _isControlOrMetaPressed)
        {
            e.Handled = true;
            return;
        }

        if (!string.IsNullOrEmpty(e.Text) && e.Text != "\b" && !e.Handled)
        {
            // Close completion popup on space
            if (e.Text == " " && _viewModel.IsCompletionActive) _viewModel.CloseCompletion();

            // Delete the selected text if there was a selection
            if (_selectionManager.HasSelection) DeleteSelectedText();

            HandleTextInput(e.Text);
            _textAnalysisService.ResetDesiredColumn();
            e.Handled = true;
        }

        // Show completion popup for '?' or letter/digit
        if (e.Text == "?" || char.IsLetterOrDigit(e.Text[0])) await _viewModel.TriggerCompletionAsync();

        _selectionManager.ClearSelection();
    }

    public void SetViewModel(IEditorViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    private void HandleCompletionKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                _viewModel.MoveCompletionSelection(-1);
                e.Handled = true;
                break;
            case Key.Down:
                _viewModel.MoveCompletionSelection(1);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Tab:
                _viewModel.ApplySelectedCompletion();
                e.Handled = true;
                break;
            case Key.Escape:
                _viewModel.CloseCompletion();
                e.Handled = true;
                break;
        }
    }

    private void HandleSelectAll()
    {
        var documentLength = _textBufferService.GetLength();
        _selectionManager.StartSelection(0);
        _selectionManager.ExtendSelection(documentLength);
        _cursorManager.SetPosition(documentLength);
        _textAnalysisService.ResetDesiredColumn();
    }

    private void HandleEnterKey()
    {
        if (_selectionManager.HasSelection) DeleteSelectedText();
        InsertTextAndMoveCursor("\n", 1);
        _textAnalysisService.ResetDesiredColumn();
    }

    private void HandleTabKey()
    {
        if (_selectionManager.HasSelection)
        {
            var selection = _selectionManager.CurrentSelection;
            var selectedText = _selectionManager.GetSelectedText(_textBufferService);
            var indentedText = IndentText(selectedText);
            ReplaceSelectedText(indentedText);
            _selectionManager.StartSelection(selection.Start);
            _selectionManager.ExtendSelection(selection.Start + indentedText.Length);
        }
        else
        {
            InsertTextAndMoveCursor("    ", 4);
        }

        _textAnalysisService.ResetDesiredColumn();
    }

    private void HandleShiftTabKey()
    {
        if (_selectionManager.HasSelection)
        {
            var selection = _selectionManager.CurrentSelection;
            var selectedText = _selectionManager.GetSelectedText(_textBufferService);
            var unindentedText = UnindentTextBySpacesOrTabs(selectedText);
            ReplaceSelectedText(unindentedText);
            _selectionManager.StartSelection(selection.Start);
            _selectionManager.ExtendSelection(selection.Start + unindentedText.Length);
        }
        else
        {
            var currentLineStart =
                _textAnalysisService.FindStartOfCurrentLine(_textBufferService.GetContent(), _cursorManager.Position);
            var currentLineEnd =
                _textAnalysisService.FindEndOfCurrentLine(_textBufferService.GetContent(), _cursorManager.Position);
            var currentLineText =
                _textBufferService.GetContentSlice(currentLineStart, currentLineEnd - currentLineStart);
            var unindentedLineText = UnindentTextBySpacesOrTabs(currentLineText);
            var diff = currentLineText.Length - unindentedLineText.Length;
            _textBufferService.Replace(currentLineStart, currentLineEnd - currentLineStart, unindentedLineText);
            _cursorManager.MoveCursor(-Math.Min(diff, _cursorManager.Position - currentLineStart));
        }

        _textAnalysisService.ResetDesiredColumn();
    }

    private string UnindentTextBySpacesOrTabs(string text)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var charsToRemove = 0;

            // Check for tabs first
            if (line.StartsWith('\t'))
                charsToRemove = 1;
            // Then check for spaces (up to 4)
            else
                for (var j = 0; j < Math.Min(line.Length, 4); j++)
                    if (line[j] == ' ')
                        charsToRemove++;
                    else
                        break;

            // Remove the leading whitespace
            if (charsToRemove > 0) lines[i] = line.Substring(charsToRemove);
        }

        return string.Join('\n', lines);
    }

    private string IndentText(string text)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++) lines[i] = "    " + lines[i];
        return string.Join('\n', lines);
    }

    private string UnindentTextBySpaces(string text, int spaces)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var spacesToRemove = 0;
            for (var j = 0; j < Math.Min(lines[i].Length, spaces); j++)
                if (lines[i][j] == ' ')
                    spacesToRemove++;
                else
                    break;
            if (spacesToRemove > 0) lines[i] = lines[i].Substring(spacesToRemove);
        }

        return string.Join('\n', lines);
    }

    private void ReplaceSelectedText(string newText)
    {
        var selection = _selectionManager.CurrentSelection;
        var oldText = _textBufferService.GetContentSlice(selection.Start, selection.End - selection.Start);
        _viewModel.RecordChange(new TextChange(selection.Start, oldText.Length, newText.Length, newText, oldText));
        _textBufferService.DeleteText(selection.Start, selection.End - selection.Start);
        _textBufferService.InsertText(selection.Start, newText);
        _cursorManager.SetPosition(selection.Start + newText.Length);
        _selectionManager.ClearSelection();
    }

    private void HandleLeftKey()
    {
        if (_isControlOrMetaPressed)
        {
            var text = _textBufferService.GetContent();
            var newPosition = _textAnalysisService.FindPreviousWordBoundary(text, _cursorManager.Position);
            UpdateCursorAndSelection(newPosition);
        }
        else if (_selectionManager.HasSelection && !_isShiftPressed)
        {
            _cursorManager.SetPosition(_selectionManager.CurrentSelection.Start);
            _selectionManager.ClearSelection();
        }
        else
        {
            var newPosition = Math.Max(0, _cursorManager.Position - 1);
            UpdateCursorAndSelection(newPosition);
        }

        UpdateDesiredColumn();
    }

    private void HandleRightKey()
    {
        if (_isControlOrMetaPressed)
        {
            var text = _textBufferService.GetContent();
            var newPosition = _textAnalysisService.FindNextWordBoundary(text, _cursorManager.Position);
            UpdateCursorAndSelection(newPosition);
        }
        else if (_selectionManager.HasSelection && !_isShiftPressed)
        {
            _cursorManager.SetPosition(_selectionManager.CurrentSelection.End);
            _selectionManager.ClearSelection();
        }
        else
        {
            var newPosition = Math.Min(_textBufferService.GetLength(), _cursorManager.Position + 1);
            UpdateCursorAndSelection(newPosition);
        }

        UpdateDesiredColumn();
    }

    private void HandleUpKey()
    {
        if (_selectionManager.HasSelection && !_isShiftPressed)
        {
            _cursorManager.SetPosition(_selectionManager.CurrentSelection.Start);
            _selectionManager.ClearSelection();
            UpdateDesiredColumn();
        }
        else
        {
            var text = _textBufferService.GetContent();
            var newPosition = _textAnalysisService.FindPositionInLineAbove(text, _cursorManager.Position);
            UpdateCursorAndSelection(newPosition);
        }
    }

    private void HandleDownKey()
    {
        if (_selectionManager.HasSelection && !_isShiftPressed)
        {
            _cursorManager.SetPosition(_selectionManager.CurrentSelection.End);
            _selectionManager.ClearSelection();
            UpdateDesiredColumn();
        }
        else
        {
            var text = _textBufferService.GetContent();
            var newPosition = _textAnalysisService.FindPositionInLineBelow(text, _cursorManager.Position);
            UpdateCursorAndSelection(newPosition);
        }
    }

    private void HandleHomeKey()
    {
        var text = _textBufferService.GetContent();
        var newPosition = _textAnalysisService.FindStartOfCurrentLine(text, _cursorManager.Position);
        UpdateCursorAndSelection(newPosition);
        UpdateDesiredColumn();
    }

    private void HandleEndKey()
    {
        var text = _textBufferService.GetContent();
        var newPosition = _textAnalysisService.FindEndOfCurrentLine(text, _cursorManager.Position);
        UpdateCursorAndSelection(newPosition);
        UpdateDesiredColumn();
    }

    private void HandleCtrlHomeKey()
    {
        UpdateCursorAndSelection(0);
        UpdateDesiredColumn();
    }

    private void HandleCtrlEndKey()
    {
        var newPosition = _textBufferService.GetLength();
        UpdateCursorAndSelection(newPosition);
        UpdateDesiredColumn();
    }

    private void HandlePageUpKey()
    {
        MoveCursorToVisiblePosition(-1);
    }

    private void HandlePageDownKey()
    {
        MoveCursorToVisiblePosition(1);
    }

    private void MoveCursorToVisiblePosition(int direction)
    {
        var text = _textBufferService.GetContent();
        var currentPosition = _cursorManager.Position;
        var currentLine = _textAnalysisService.GetLineNumber(text, currentPosition);
        var lineCount = _textAnalysisService.GetLineCount(text);
        var visibleLineCount = _scrollManager.GetVisibleLineCount();

        // Calculate the target line
        int targetLine;
        if (direction > 0) // Page Down
            targetLine = Math.Min(currentLine + visibleLineCount, lineCount - 1);
        else // Page Up
            targetLine = Math.Max(currentLine - visibleLineCount, 0);

        // Calculate the new cursor position
        var targetPosition = _textAnalysisService.GetPositionFromLine(text, targetLine);
        var lineStart = _textAnalysisService.FindStartOfCurrentLine(text, targetPosition);
        var lineEnd = _textAnalysisService.GetEndOfLine(text, targetLine);

        // Maintain the horizontal position (column) if possible
        var currentColumn = currentPosition - _textAnalysisService.FindStartOfCurrentLine(text, currentPosition);
        var newPosition = Math.Min(lineStart + currentColumn, lineEnd);

        // Update cursor and selection
        UpdateCursorAndSelection(newPosition);

        // Update the desired column
        _textAnalysisService.SetDesiredColumn(currentColumn);
    }

    private void HandleScrollUpKey()
    {
        _scrollManager.ScrollUp();
    }

    private void HandleScrollDownKey()
    {
        _scrollManager.ScrollDown();
    }

    private void UpdateDesiredColumn()
    {
        var text = _textBufferService.GetContent();
        var lineStart = _textAnalysisService.FindStartOfCurrentLine(text, _cursorManager.Position);
        var desiredColumn = _cursorManager.Position - lineStart;
        _textAnalysisService.SetDesiredColumn(desiredColumn);
    }

    private void UpdateCursorAndSelection(int newPosition)
    {
        _selectionManager.HandleKeyboardSelection(newPosition, _isShiftPressed);
        _cursorManager.SetPosition(newPosition);
    }

    private async Task HandleBackspaceKey()
    {
        if (_selectionManager.HasSelection)
        {
            var selection = _selectionManager.CurrentSelection;
            var deletedText = _selectionManager.GetSelectedText(_textBufferService);
            _viewModel.RecordChange(new TextChange(selection.Start, deletedText.Length, 0, "", deletedText));
            _textBufferService.DeleteText(selection.Start, selection.End - selection.Start);
            _cursorManager.SetPosition(selection.Start);
            _selectionManager.ClearSelection();
        }
        else if (_cursorManager.Position > 0)
        {
            var position = _cursorManager.Position - 1;
            var deletedText = _textBufferService.GetContentSliceByIndex(position, 1);
            _viewModel.RecordChange(new TextChange(position, 1, 0, "", deletedText));
            _textBufferService.DeleteText(position, 1);
            _cursorManager.MoveCursor(-1);
        }

        _textAnalysisService.ResetDesiredColumn();

        if (IsWordBehindCursor())
            await _viewModel.TriggerCompletionAsync();
        else
            _viewModel.CloseCompletion();
    }

    private bool IsWordBehindCursor()
    {
        var text = _textBufferService.GetContent();
        var cursorPosition = _cursorManager.Position;

        // Check if there's at least one character behind the cursor
        if (cursorPosition > 0)
        {
            // Get the character immediately before the cursor
            var prevChar = text[cursorPosition - 1];

            // Check if the previous character is a letter, digit, or underscore
            return char.IsLetterOrDigit(prevChar) || prevChar == '_';
        }

        return false;
    }

    private async Task HandleDeleteKey()
    {
        if (_selectionManager.HasSelection)
        {
            DeleteSelectedText();
        }
        else if (_cursorManager.Position < _textBufferService.GetLength())
        {
            var position = _cursorManager.Position;
            var deletedText = _textBufferService.GetContentSliceByIndex(position, 1);
            _viewModel.RecordChange(new TextChange(position, 1, 0, "", deletedText));
            _textBufferService.DeleteText(position, 1);
        }

        _textAnalysisService.ResetDesiredColumn();

        if (IsWordBehindCursor())
            await _viewModel.TriggerCompletionAsync();
        else
            _viewModel.CloseCompletion();
    }

    private async Task HandleClipboardOperation(Key key)
    {
        switch (key)
        {
            case Key.X:
                await CutAsync();
                break;
            case Key.C:
                await CopyAsync();
                break;
            case Key.V:
                await PasteAsync();
                break;
        }
    }

    private async Task CutAsync()
    {
        if (_selectionManager.HasSelection)
        {
            var selection = _selectionManager.CurrentSelection;
            var selectedText = _selectionManager.GetSelectedText(_textBufferService);
            await _clipboardManager.CopyAsync(selectedText);

            // Record the change before deleting the text
            _viewModel.RecordChange(new TextChange(selection.Start, selectedText.Length, 0, "", selectedText));

            // Delete the selected text
            _textBufferService.DeleteText(selection.Start, selection.End - selection.Start);
            _cursorManager.SetPosition(selection.Start);
            _selectionManager.ClearSelection();
            _textAnalysisService.ResetDesiredColumn();
        }
    }

    private async Task CopyAsync()
    {
        if (_selectionManager.HasSelection)
        {
            var selectedText = _selectionManager.GetSelectedText(_textBufferService);
            await _clipboardManager.CopyAsync(selectedText);
        }
    }

    private async Task PasteAsync()
    {
        var clipboardText = await _clipboardManager.PasteAsync();
        if (!string.IsNullOrEmpty(clipboardText))
        {
            if (_selectionManager.HasSelection)
            {
                var selection = _selectionManager.CurrentSelection;
                var oldText = _textBufferService.GetContentSlice(selection.Start, selection.End - selection.Start);
                _viewModel.RecordChange(new TextChange(selection.Start, oldText.Length, clipboardText.Length, clipboardText, oldText));
                DeleteSelectedText();
            }
            else
            {
                var position = _cursorManager.Position;
                _viewModel.RecordChange(new TextChange(position, 0, clipboardText.Length, clipboardText, ""));
            }
            InsertTextAndMoveCursor(clipboardText, clipboardText.Length);
            _textAnalysisService.ResetDesiredColumn();
        }
    }

    private void InsertTextAndMoveCursor(string text, int offset)
    {
        var currentPosition = _cursorManager.Position;
        _textBufferService.InsertText(currentPosition, text);
        _cursorManager.MoveCursor(offset);
    }

    private void DeleteSelectedText()
    {
        if (!_selectionManager.HasSelection) return;

        var selection = _selectionManager.CurrentSelection;
        var content = _textBufferService.GetContent();

        // Ensure selection bounds are within the content range
        var start = Math.Max(0, Math.Min(selection.Start, content.Length));
        var end = Math.Max(start, Math.Min(selection.End, content.Length));

        if (start != end)
        {
            var deletedText = _textBufferService.GetContentSlice(start, end - start);
            _viewModel.RecordChange(new TextChange(start, end - start, 0, "", deletedText));
            _textBufferService.DeleteText(start, end - start);
            _cursorManager.SetPosition(start);
        }

        _selectionManager.ClearSelection();
    }

    private void HandleTextInput(string text)
    {
        if (_selectionManager.HasSelection)
        {
            var selection = _selectionManager.CurrentSelection;
            var oldText = _textBufferService.GetContentSlice(selection.Start, selection.End - selection.Start);
            _viewModel.RecordChange(new TextChange(selection.Start, oldText.Length, text.Length, text, oldText));
            ReplaceSelectedText(text);
        }
        else
        {
            var position = _cursorManager.Position;
            _viewModel.RecordChange(new TextChange(position, 0, text.Length, text, ""));
            _textBufferService.InsertText(position, text);
            _cursorManager.MoveCursor(text.Length);
        }
    }
}