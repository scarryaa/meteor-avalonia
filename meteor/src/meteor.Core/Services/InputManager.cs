using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.EventArgs;

namespace meteor.Core.Services;

public class InputManager : IInputManager
{
    private readonly ITextBufferService _textBufferService;
    private readonly ICursorManager _cursorManager;
    private readonly IClipboardManager _clipboardManager;
    private bool _isClipboardOperationHandled;

    public InputManager(ITextBufferService textBufferService, ICursorManager cursorManager,
        IClipboardManager clipboardManager)
    {
        _textBufferService = textBufferService ?? throw new ArgumentNullException(nameof(textBufferService));
        _cursorManager = cursorManager ?? throw new ArgumentNullException(nameof(cursorManager));
        _clipboardManager = clipboardManager ?? throw new ArgumentNullException(nameof(clipboardManager));
    }

    public async Task HandleKeyDown(KeyEventArgs e)
    {
        try
        {
            _isClipboardOperationHandled = false;

            switch (e.Key)
            {
                case Key.Enter:
                    InsertTextAndMoveCursor("\n", 1);
                    e.Handled = true;
                    break;
                case Key.Left:
                    _cursorManager.MoveCursor(-1);
                    e.Handled = true;
                    break;
                case Key.Right:
                    _cursorManager.MoveCursor(1);
                    e.Handled = true;
                    break;
                case Key.Back:
                    if (_cursorManager.Position > 0)
                    {
                        _textBufferService.DeleteText(_cursorManager.Position - 1, 1);
                        _cursorManager.MoveCursor(-1);
                    }

                    e.Handled = true;
                    break;
                case Key.X:
                case Key.C:
                case Key.V:
                    if (e.Modifiers.HasFlag(KeyModifiers.Control) || e.Modifiers.HasFlag(KeyModifiers.Meta))
                    {
                        await HandleClipboardOperation(e.Key);
                        e.Handled = true;
                        _isClipboardOperationHandled = true;
                    }

                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling key down event: {ex.Message}");
        }
    }

    public void HandleTextInput(TextInputEventArgs e)
    {
        if (_isClipboardOperationHandled)
        {
            e.Handled = true;
            return;
        }

        if (!string.IsNullOrEmpty(e.Text) && e.Text != "\b" && !e.Handled)
            try
            {
                InsertTextAndMoveCursor(e.Text, e.Text.Length);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling text input event: {ex.Message}");
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
    }

    private async Task CopyAsync()
    {
    }

    private async Task PasteAsync()
    {
        var clipboardText = await _clipboardManager.PasteAsync();
        if (!string.IsNullOrEmpty(clipboardText)) InsertTextAndMoveCursor(clipboardText, clipboardText.Length);
    }

    private void InsertTextAndMoveCursor(string text, int offset)
    {
        var currentPosition = _cursorManager.Position;
        _textBufferService.InsertText(currentPosition, text);
        _cursorManager.MoveCursor(offset);
    }
}