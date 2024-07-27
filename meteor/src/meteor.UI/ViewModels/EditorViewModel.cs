using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.EventArgs;

namespace meteor.UI.ViewModels;

public class EditorViewModel : IEditorViewModel
{
    private readonly ICursorManager _cursorManager;
    private readonly IInputManager _inputManager;
    private readonly ISelectionManager _selectionManager;
    private readonly IEditorConfig _config;
    private readonly ITextMeasurer _textMeasurer;

    public event EventHandler? ContentChanged;
    public event EventHandler? SelectionChanged;

    public EditorViewModel(
        ITextBufferService textBufferService,
        ICursorManager cursorManager,
        IInputManager inputManager,
        ISelectionManager selectionManager,
        IEditorConfig config,
        ITextMeasurer textMeasurer)
    {
        TextBufferService = textBufferService;
        _cursorManager = cursorManager;
        _inputManager = inputManager;
        _selectionManager = selectionManager;
        _config = config;
        _textMeasurer = textMeasurer;

        _cursorManager.CursorPositionChanged += (_, _) => ContentChanged?.Invoke(this, EventArgs.Empty);
        _selectionManager.SelectionChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public ITextBufferService TextBufferService { get; }

    public int SelectionStart => _selectionManager.CurrentSelection.Start;
    public int SelectionEnd => _selectionManager.CurrentSelection.End;

    public string Content { get; set; }
    public int GetLineCount()
    {
        return TextBufferService.GetLineCount();
    }

    public int GetDocumentLength()
    {
        return TextBufferService.GetLength();
    }

    public int GetDocumentVersion()
    {
        return TextBufferService.GetDocumentVersion();
    }

    public double GetMaxLineWidth()
    {
        return TextBufferService.GetMaxLineWidth(_config.FontFamily, _config.FontSize);
    }

    public string GetContentSlice(int start, int end)
    {
        return TextBufferService.GetContentSlice(start, end);
    }

    public int GetCursorLine()
    {
        return _cursorManager.GetCursorLine();
    }

    public int GetCursorColumn()
    {
        return _cursorManager.GetCursorColumn();
    }

    public double GetCursorX()
    {
        var cursorLine = _cursorManager.GetCursorLine();
        var cursorColumn = _cursorManager.GetCursorColumn();
        var lineContent = TextBufferService.GetContentSlice(cursorLine, cursorLine);

        cursorColumn = Math.Min(cursorColumn, lineContent.Length);
        var textUpToCursor = lineContent.Substring(0, cursorColumn);

        return _textMeasurer.MeasureText(textUpToCursor, _config.FontFamily, _config.FontSize).Width;
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

    public void StartSelection(int position)
    {
        _selectionManager.StartSelection(position);
        _cursorManager.SetPosition(position);
    }

    public void UpdateSelection(int position)
    {
        _selectionManager.ExtendSelection(position);
        _cursorManager.SetPosition(position);
    }

    public void EndSelection()
    {
        // This method is left empty as per the original implementation
    }

    public bool HasSelection()
    {
        return _selectionManager.HasSelection;
    }

    public void SetCursorPosition(int position)
    {
        _cursorManager.SetPosition(position);
    }

    public int GetLineStartOffset(int lineIndex)
    {
        return TextBufferService.GetLineStartOffset(lineIndex);
    }
}