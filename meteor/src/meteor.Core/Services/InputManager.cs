using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.EventArgs;

namespace meteor.Core.Services;

public class InputManager : IInputManager
{
    private readonly ITextBufferService _textBufferService;
    private readonly ICursorManager _cursorManager;
    private readonly IClipboardManager _clipboardManager;
    private readonly ISelectionManager _selectionManager;
    private readonly ITextAnalysisService _textAnalysisService;
    private readonly IScrollManager _scrollManager;
    private bool _isClipboardOperationHandled;
    private bool _isShiftPressed;
    private bool _isControlOrMetaPressed;
    private bool _isAltPressed;
    private bool _isSelectionInProgress;
    private (int Start, int End) _lastSelection;

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
                    HandleBackspaceKey();
                    break;
                case Key.Delete:
                    HandleDeleteKey();
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
                    break;
                case Key.A:
                    if (_isControlOrMetaPressed)
                        HandleSelectAll();
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    // Do nothing for Alt key presses
                    break;
                default:
                    return;
            }

            e.Handled = true;

            if (!_isShiftPressed && !_isControlOrMetaPressed && !_isAltPressed &&
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

    public void HandleTextInput(TextInputEventArgs e)
    {
        if (_isClipboardOperationHandled || _isControlOrMetaPressed)
        {
            e.Handled = true;
            return;
        }

        if (!string.IsNullOrEmpty(e.Text) && e.Text != "\b" && !e.Handled)
        {
            // Delete the selected text if there was a selection
            if (_lastSelection.Start != _lastSelection.End)
            {
                _textBufferService.DeleteText(_lastSelection.Start, _lastSelection.End - _lastSelection.Start);
                _cursorManager.SetPosition(_lastSelection.Start);
            }

            InsertTextAndMoveCursor(e.Text, e.Text.Length);
            _textAnalysisService.ResetDesiredColumn();
            e.Handled = true;
        }

        _lastSelection = (0, 0);
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
            var text = _textBufferService.GetEntireContent();
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
            var text = _textBufferService.GetEntireContent();
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
            var text = _textBufferService.GetEntireContent();
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
            var text = _textBufferService.GetEntireContent();
            var newPosition = _textAnalysisService.FindPositionInLineBelow(text, _cursorManager.Position);
            UpdateCursorAndSelection(newPosition);
        }
    }

    private void HandleDeleteKey()
    {
        if (_selectionManager.HasSelection)
            DeleteSelectedText();
        else if (_cursorManager.Position < _textBufferService.GetLength())
            _textBufferService.DeleteText(_cursorManager.Position, 1);
        UpdateDesiredColumn();
    }

    private void HandleHomeKey()
    {
        var text = _textBufferService.GetEntireContent();
        var newPosition = _textAnalysisService.FindStartOfCurrentLine(text, _cursorManager.Position);
        UpdateCursorAndSelection(newPosition);
        UpdateDesiredColumn();
    }

    private void HandleEndKey()
    {
        var text = _textBufferService.GetEntireContent();
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
        var text = _textBufferService.GetEntireContent();
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

        // Enhanced logging for debugging
        Console.WriteLine(
            $"Target line: {targetLine}, New position: {newPosition}, Current line: {currentLine}, " +
            $"Visible lines: {visibleLineCount}, Viewport: {_scrollManager.Viewport}, " +
            $"Extent size: {_scrollManager.ExtentSize}, Scroll offset: {_scrollManager.ScrollOffset}");
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
        var text = _textBufferService.GetEntireContent();
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

    private void HandleBackspaceKey()
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
        var selection = _selectionManager.CurrentSelection;
        _textBufferService.DeleteText(selection.Start, selection.End - selection.Start);
        _cursorManager.SetPosition(selection.Start);
        _selectionManager.ClearSelection();
    }
}