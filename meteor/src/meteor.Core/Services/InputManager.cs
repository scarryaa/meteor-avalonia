using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.EventArgs;

namespace meteor.Core.Services;

public class InputManager : IInputManager
{
    private readonly ITextBufferService _textBufferService;
    private readonly ICursorManager _cursorManager;

    public InputManager(ITextBufferService textBufferService, ICursorManager cursorManager)
    {
        _textBufferService = textBufferService;
        _cursorManager = cursorManager;
    }

    public void HandleKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                _textBufferService.InsertText(_cursorManager.Position, "\n");
                _cursorManager.MoveCursor(1);
                break;
            case Key.Left:
                _cursorManager.MoveCursor(-1);
                break;
            case Key.Right:
                _cursorManager.MoveCursor(1);
                break;
            case Key.Back:
                if (_cursorManager.Position > 0)
                {
                    _textBufferService.DeleteText(_cursorManager.Position - 1, 1);
                    _cursorManager.MoveCursor(-1);
                }

                break;
        }
    }

    public void HandleTextInput(TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text))
        {
            _textBufferService.InsertText(_cursorManager.Position, e.Text);
            _cursorManager.MoveCursor(e.Text.Length);
        }
    }
}