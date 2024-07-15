using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Commands;

public class TextEditorCommands : ITextEditorCommands
{
    private readonly ITextBuffer _textBuffer;
    private readonly ICursorManager _cursorManager;
    private readonly ISelectionHandler _selectionHandler;
    private readonly IClipboardService _clipboardService;
    private readonly IUndoRedoManager<ITextBuffer> _undoRedoManager;

    public TextEditorCommands(
        ITextBuffer textBuffer,
        ICursorManager cursorManager,
        ISelectionHandler selectionHandler,
        IClipboardService clipboardService,
        IUndoRedoManager<ITextBuffer> undoRedoManager)
    {
        _textBuffer = textBuffer;
        _cursorManager = cursorManager;
        _selectionHandler = selectionHandler;
        _clipboardService = clipboardService;
        _undoRedoManager = undoRedoManager;
    }

    public void InsertText(int position, string text)
    {
        if (_selectionHandler.HasSelection) DeleteSelectedText();
        _textBuffer.InsertText(position, text);
        _cursorManager.SetPosition(position + text.Length);
    }

    public void HandleBackspace()
    {
        if (_selectionHandler.HasSelection)
        {
            DeleteSelectedText();
        }
        else if (_cursorManager.Position > 0)
        {
            _textBuffer.DeleteText(_cursorManager.Position - 1, 1);
            _cursorManager.MoveCursorLeft(false);
        }
    }

    public void HandleDelete()
    {
        if (_selectionHandler.HasSelection)
            DeleteSelectedText();
        else if (_cursorManager.Position < _textBuffer.Length) _textBuffer.DeleteText(_cursorManager.Position, 1);
    }

    public void InsertNewLine()
    {
        InsertText(_cursorManager.Position, Environment.NewLine);
    }

    public async Task CopyText()
    {
        if (_selectionHandler.HasSelection)
        {
            var selectedText = _textBuffer.GetText(
                _selectionHandler.SelectionStart,
                _selectionHandler.SelectionEnd - _selectionHandler.SelectionStart
            );
            await _clipboardService.SetTextAsync(selectedText);
        }
    }

    public async Task PasteText()
    {
        var text = await _clipboardService.GetTextAsync();
        if (!string.IsNullOrEmpty(text)) InsertText(_cursorManager.Position, text);
    }

    public async Task CutText()
    {
        if (_selectionHandler.HasSelection)
        {
            await CopyText();
            DeleteSelectedText();
        }
    }

    public void Undo()
    {
        _undoRedoManager.Undo();
    }

    public void Redo()
    {
        _undoRedoManager.Redo();
    }

    public int GetPositionFromPoint(IPoint point)
    {
        // TODO implement
        return 0;
    }

    private void DeleteSelectedText()
    {
        if (_selectionHandler.HasSelection)
        {
            _textBuffer.DeleteText(
                _selectionHandler.SelectionStart,
                _selectionHandler.SelectionEnd - _selectionHandler.SelectionStart
            );
            _cursorManager.SetPosition(_selectionHandler.SelectionStart);
            _selectionHandler.ClearSelection();
        }
    }
}