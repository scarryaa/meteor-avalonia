using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.EventArgs;

namespace meteor.UI.ViewModels;

public class EditorViewModel : IEditorViewModel
{
    private readonly ITextBufferService _textBufferService;
    private readonly ICursorManager _cursorManager;
    private readonly IInputManager _inputManager;
    private readonly ISelectionManager _selectionManager;
    private readonly IEditorConfig _config;

    public event EventHandler? ContentChanged;
    public event EventHandler? SelectionChanged;
    
    public EditorViewModel(ITextBufferService textBufferService, ICursorManager cursorManager,
        IInputManager inputManager, ISelectionManager selectionManager, IEditorConfig config)
    {
        _config = config;
        _textBufferService = textBufferService;
        _cursorManager = cursorManager;
        _inputManager = inputManager;
        _selectionManager = selectionManager;

        _cursorManager.CursorPositionChanged += (_, _) => ContentChanged?.Invoke(this, EventArgs.Empty);
        _selectionManager.SelectionChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public int SelectionStart => _selectionManager.CurrentSelection.Start;
    public int SelectionEnd => _selectionManager.CurrentSelection.End;

    public int GetLineCount()
    {
        return _textBufferService.GetLineCount();
    }

    public double GetMaxLineWidth()
    {
        return _textBufferService.GetMaxLineWidth(_config.FontFamily, _config.FontSize);
    }

    public string GetContentSlice(int start, int end)
    {
        return _textBufferService.GetContentSlice(start, end);
    }

    public string GetEntireContent()
    {
        return _textBufferService.GetEntireContent();
    }

    public int GetCursorLine()
    {
        return _cursorManager.GetCursorLine();
    }

    public int GetCursorColumn()
    {
        return _cursorManager.GetCursorColumn();
    }

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