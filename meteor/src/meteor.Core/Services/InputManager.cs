using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
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
    private bool _isSelectionInProgress;
    private bool _isShiftPressed;
    private (int Start, int End) _lastSelection;
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

        if (e.Key == Key.Space && e.Modifiers == KeyModifiers.Control)
        {
            await _viewModel.TriggerCompletionAsync();
            e.Handled = true;
        }

        try
        {
            _isClipboardOperationHandled = false;
            _isShiftPressed = e.Modifiers.HasFlag(KeyModifiers.Shift);
            _isControlOrMetaPressed =
                e.Modifiers.HasFlag(KeyModifiers.Control) || e.Modifiers.HasFlag(KeyModifiers.Meta);
            _isAltPressed = e.Modifiers.HasFlag(KeyModifiers.Alt);

            if (_isShiftPressed && !_isSelectionInProgress)
            {
                _selectionManager.StartSelection(_cursorManager.Position);
                _isSelectionInProgress = true;
            }

            switch (e.Key)
            {
                case Key.Enter:
                    HandleEnterKey();
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
                case Key.LeftAlt:
                case Key.RightAlt:
                    // Do nothing for Alt key presses
                    break;
                default:
                    return;
            }

            e.Handled = true;

            if (!_isControlOrMetaPressed && !_isAltPressed && !_isShiftPressed &&
                e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.Up && e.Key != Key.Down &&
                e.Key != Key.LeftShift && e.Key != Key.RightShift &&
                e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl &&
                e.Key != Key.LeftAlt && e.Key != Key.RightAlt &&
                e.Key != Key.LeftMeta && e.Key != Key.RightMeta)
            {
                _lastSelection = (_selectionManager.CurrentSelection.Start, _selectionManager.CurrentSelection.End);
                _selectionManager.ClearSelection();
                _isSelectionInProgress = false;
            }
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

            InsertTextAndMoveCursor(e.Text, e.Text.Length);
            _textAnalysisService.ResetDesiredColumn();
            e.Handled = true;
        }

        // Show completion popup for '?' or letter/digit
        if (e.Text == "?" || char.IsLetterOrDigit(e.Text[0])) await _viewModel.TriggerCompletionAsync();

        _lastSelection = (0, 0);
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
        _selectionManager.SetSelection(0, documentLength);
        _cursorManager.SetPosition(documentLength);
        _isSelectionInProgress = true;
        _textAnalysisService.ResetDesiredColumn();
    }

    private void HandleEnterKey()
    {
        if (_selectionManager.HasSelection) DeleteSelectedText();
        InsertTextAndMoveCursor("\n", 1);
        _textAnalysisService.ResetDesiredColumn();
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

    private async Task HandleDeleteKey()
    {
        if (_selectionManager.HasSelection)
            DeleteSelectedText();
        else if (_cursorManager.Position < _textBufferService.GetLength())
            _textBufferService.DeleteText(_cursorManager.Position, 1);
        UpdateDesiredColumn();

        if (IsWordBehindCursor())
            await _viewModel.TriggerCompletionAsync();
        else
            _viewModel.CloseCompletion();
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
        if (_isShiftPressed)
            _selectionManager.ExtendSelection(newPosition);
        else
            _selectionManager.ClearSelection();
        _cursorManager.SetPosition(newPosition);
    }

    private async Task HandleBackspaceKey()
    {
        if (_selectionManager.HasSelection)
        {
            DeleteSelectedText();
        }
        else if (_cursorManager.Position > 0)
        {
            _textBufferService.DeleteText(_cursorManager.Position - 1, 1);
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
            var selectedText = _selectionManager.GetSelectedText(_textBufferService);
            await _clipboardManager.CopyAsync(selectedText);
            DeleteSelectedText();
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
            if (_selectionManager.HasSelection) DeleteSelectedText();
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
            _textBufferService.DeleteText(start, end - start);
            _cursorManager.SetPosition(start);
        }

        _selectionManager.ClearSelection();
    }
}