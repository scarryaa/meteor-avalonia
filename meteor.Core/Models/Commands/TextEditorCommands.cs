using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Interfaces.Contexts;
using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Models.Commands;

public class TextEditorCommands(
    ITextBuffer textBuffer,
    ICursorManager cursorManager,
    ISelectionHandler selectionHandler,
    IClipboardService clipboardService,
    IUndoRedoManager<ITextBuffer> undoRedoManager,
    ITextEditorContext context,
    ITextMeasurer textMeasurer)
    : ITextEditorCommands
{
    public void InsertText(int position, string text)
    {
        if (selectionHandler.HasSelection) DeleteSelectedText();
        textBuffer.InsertText(position, text);
        cursorManager.SetPosition(position + text.Length);
    }

    public void HandleBackspace()
    {
        if (selectionHandler.HasSelection)
        {
            DeleteSelectedText();
        }
        else if (cursorManager.Position > 0)
        {
            textBuffer.DeleteText(cursorManager.Position - 1, 1);
            cursorManager.MoveCursorLeft(false);
        }
    }

    public void HandleDelete()
    {
        if (selectionHandler.HasSelection)
            DeleteSelectedText();
        else if (cursorManager.Position < textBuffer.Length) textBuffer.DeleteText(cursorManager.Position, 1);
    }

    public void InsertNewLine()
    {
        InsertText(cursorManager.Position, Environment.NewLine);
    }

    public async Task CopyText()
    {
        if (selectionHandler.HasSelection)
        {
            var selectedText = textBuffer.GetText(
                selectionHandler.SelectionStart,
                selectionHandler.SelectionEnd - selectionHandler.SelectionStart
            );
            await clipboardService.SetTextAsync(selectedText);
        }
    }

    public async Task PasteText()
    {
        var text = await clipboardService.GetTextAsync();
        Console.WriteLine($"Pasted: {text}");
        if (!string.IsNullOrEmpty(text)) InsertText(cursorManager.Position, text);
    }

    public async Task CutText()
    {
        if (selectionHandler.HasSelection)
        {
            await CopyText();
            DeleteSelectedText();
        }
    }

    public void Undo()
    {
        undoRedoManager.Undo();
    }

    public void Redo()
    {
        undoRedoManager.Redo();
    }

    public int GetPositionFromPoint(IPoint? point)
    {
        if (point != null)
        {
            var line = Math.Min(Math.Max((int)(point.Y / context.LineHeight), 0), textBuffer.LineCount - 1);
            var lineText = textBuffer.GetLineText(line);
            var maxWidth = textMeasurer.MeasureWidth(lineText, context.FontSize, context.FontFamily.Name);
            var column = Math.Min(Math.Max((int)(point.X * lineText.Length / maxWidth), 0), lineText.Length);

            var position = 0;
            for (var i = 0; i < line; i++) position += textBuffer.GetLineLength(i) + 1;

            position += column;

            return Math.Min(position, textBuffer.Length);
        }

        throw new ArgumentNullException(nameof(point));
    }

    private void DeleteSelectedText()
    {
        if (selectionHandler.HasSelection)
        {
            textBuffer.DeleteText(
                selectionHandler.SelectionStart,
                selectionHandler.SelectionEnd - selectionHandler.SelectionStart
            );
            cursorManager.SetPosition(selectionHandler.SelectionStart);
            selectionHandler.ClearSelection();
        }
    }
}