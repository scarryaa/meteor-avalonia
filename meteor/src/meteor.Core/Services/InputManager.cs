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
    private bool _isClipboardOperationHandled;
    private bool _isShiftPressed;
    private bool _isControlOrMetaPressed;
    private bool _isAltPressed;
    private bool _isSelectionInProgress;
    private (int Start, int End) _lastSelection;

    public InputManager(ITextBufferService textBufferService, ICursorManager cursorManager,
        IClipboardManager clipboardManager, ISelectionManager selectionManager)
    {
        _textBufferService = textBufferService ?? throw new ArgumentNullException(nameof(textBufferService));
        _cursorManager = cursorManager ?? throw new ArgumentNullException(nameof(cursorManager));
        _clipboardManager = clipboardManager ?? throw new ArgumentNullException(nameof(clipboardManager));
        _selectionManager = selectionManager ?? throw new ArgumentNullException(nameof(selectionManager));
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
                    e.Handled = true;
                    break;
                case Key.Left:
                    HandleLeftKey();
                    e.Handled = true;
                    break;
                case Key.Right:
                    HandleRightKey();
                    e.Handled = true;
                    break;
                case Key.Back:
                    HandleBackspaceKey();
                    e.Handled = true;
                    break;
                case Key.X:
                case Key.C:
                case Key.V:
                    if (_isControlOrMetaPressed)
                    {
                        await HandleClipboardOperation(e.Key);
                        e.Handled = true;
                        _isClipboardOperationHandled = true;
                    }

                    break;
                case Key.A:
                    if (_isControlOrMetaPressed)
                    {
                        HandleSelectAll();
                        e.Handled = true;
                    }

                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    // Do nothing for Alt key presses
                    e.Handled = true;
                    break;
            }

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

            // Debug output
            Console.WriteLine($"Cursor: {_cursorManager.Position}");
            Console.WriteLine(
                $"Selection: {_selectionManager.CurrentSelection.Start} - {_selectionManager.CurrentSelection.End}");
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
    }

    private void HandleEnterKey()
    {
        if (_selectionManager.HasSelection) DeleteSelectedText();
        InsertTextAndMoveCursor("\n", 1);
    }

    private void HandleLeftKey()
    {
        var newPosition = Math.Max(0, _cursorManager.Position - 1);

        if (_isShiftPressed)
        {
            _selectionManager.ExtendSelection(newPosition);
        }
        else if (_selectionManager.HasSelection)
        {
            newPosition = _selectionManager.CurrentSelection.Start;
            _selectionManager.ClearSelection();
        }

        _cursorManager.SetPosition(newPosition);
    }

    private void HandleRightKey()
    {
        var newPosition = Math.Min(_textBufferService.GetLength(), _cursorManager.Position + 1);

        if (_isShiftPressed)
        {
            _selectionManager.ExtendSelection(newPosition);
        }
        else if (_selectionManager.HasSelection)
        {
            newPosition = _selectionManager.CurrentSelection.End;
            _selectionManager.ClearSelection();
        }

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