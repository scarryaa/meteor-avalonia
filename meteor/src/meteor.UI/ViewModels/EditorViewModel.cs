using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.EventArgs;

namespace meteor.UI.ViewModels;

public class EditorViewModel : IEditorViewModel
{
    private readonly ITextBufferService _textBufferService;
    private readonly ICursorManager _cursorManager;
    private readonly IInputManager _inputManager;

    public EditorViewModel(ITextBufferService textBufferService, ICursorManager cursorManager,
        IInputManager inputManager)
    {
        _textBufferService = textBufferService;
        _cursorManager = cursorManager;
        _inputManager = inputManager;
    }

    public string Content => _textBufferService.GetContent();

    public int CursorPosition => _cursorManager.Position;

    public void HandleKeyDown(KeyEventArgs e)
    {
        _inputManager.HandleKeyDown(e);
    }

    public void HandleTextInput(TextInputEventArgs e)
    {
        _inputManager.HandleTextInput(e);
    }
}