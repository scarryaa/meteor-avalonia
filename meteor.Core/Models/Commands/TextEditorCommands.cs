using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Interfaces.Contexts;
using meteor.Core.Interfaces.Events;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Models.Events;

namespace meteor.Core.Models.Commands;

public class TextEditorCommands(
    ITextBuffer textBuffer,
    ICursorManager cursorManager,
    ISelectionHandler selectionHandler,
    IClipboardService clipboardService,
    IUndoRedoManager<ITextBuffer> undoRedoManager,
    ITextEditorContext context,
    ITextMeasurer textMeasurer,
    IEventAggregator eventAggregator)
    : ITextEditorCommands
{
    public event EventHandler? TextChanged;

    public void InsertText(int position, string text)                           
    {
        if (selectionHandler.HasSelection) DeleteSelectedText();
        textBuffer.InsertText(position, text);
        cursorManager.SetPosition(position + text.Length);
        PublishTextChangedEvent(position, text, 0);
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
            PublishTextChangedEvent(cursorManager.Position, "", 1);
        }
    }

    public void HandleDelete()
    {
        if (selectionHandler.HasSelection)
            DeleteSelectedText();
        else if (cursorManager.Position < textBuffer.Length)
        {
            textBuffer.DeleteText(cursorManager.Position, 1);
            PublishTextChangedEvent(cursorManager.Position, "", 1);
        }
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
        if (!string.IsNullOrEmpty(text))
        {
            InsertText(cursorManager.Position, text);
            PublishTextChangedEvent(cursorManager.Position, text, 0);
        }
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
        if (point == null)
            throw new ArgumentNullException(nameof(point));

        var adjustedY = Math.Max(point.Y + context.VerticalOffset, 0);
        var line = Math.Min((int)(adjustedY / context.LineHeight), textBuffer.LineCount - 1);
        var lineText = textBuffer.GetLineText(line);

        if (string.IsNullOrEmpty(lineText))
            return textBuffer.GetLineStartPosition(line);

        var maxWidth = textMeasurer.MeasureWidth(lineText, context.FontSize, context.FontFamily.Name);
        var relativeX = Math.Max(Math.Min(point.X, maxWidth), 0);
        var column = (int)Math.Round(relativeX * lineText.Length / maxWidth);

        var position = textBuffer.GetLineStartPosition(line) + Math.Min(column, lineText.Length);
        return Math.Min(position, textBuffer.Length);
    }

    private void PublishTextChangedEvent(int position, string insertedText, int deletedLength)
    {
        eventAggregator.Publish(new TextEditorCommandTextChangedEventArgs(insertedText, position, deletedLength));
    }

    private void DeleteSelectedText()
    {
        if (selectionHandler.HasSelection)
        {
            var start = selectionHandler.SelectionStart;
            var length = selectionHandler.SelectionEnd - selectionHandler.SelectionStart;
            textBuffer.DeleteText(start, length);
            cursorManager.SetPosition(start);
            selectionHandler.ClearSelection();
            PublishTextChangedEvent(start, "", length);
        }
    }
}