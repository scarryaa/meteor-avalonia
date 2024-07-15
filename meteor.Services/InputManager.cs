using meteor.Core.Enums;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Interfaces.Events;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Models.Events;
using Microsoft.Extensions.Logging;

namespace meteor.Services;

public class InputManager(
    ICursorManager cursorManager,
    ISelectionHandler selectionHandler,
    ITextEditorCommands editorCommands,
    ILogger<InputManager> logger,
    IEventAggregator eventAggregator,
    ITextBuffer textBuffer)
    : IInputManager
{
    private IPoint? _lastClickPosition;
    private DateTime _lastClickTime;

    private const int DoubleClickTimeThreshold = 300;
    private const int TripleClickTimeThreshold = 600;
    private const double DoubleClickDistanceThreshold = 5;

    public bool IsTripleClickDrag { get; set; }
    public bool IsDoubleClickDrag { get; set; }

    public void OnPointerPressed(IPointerPressedEventArgs e)
    {
        var currentPosition = e.GetPosition();
        var currentTime = DateTime.Now;
        var textPosition = editorCommands.GetPositionFromPoint(currentPosition);

        if (IsTripleClick(currentTime, currentPosition))
        {
            selectionHandler.SelectLine(textPosition);
            IsTripleClickDrag = true;
        }
        else if (IsDoubleClick(currentTime, currentPosition))
        {
            selectionHandler.SelectWord(textPosition);
            IsDoubleClickDrag = true;
        }
        else
        {
            cursorManager.SetPosition(textPosition);
            selectionHandler.StartSelection(textPosition);

            PublishCursorPositionChanged(textPosition);
            eventAggregator.Publish(new IsSelectingChangedEventArgs(true));
        }

        _lastClickPosition = currentPosition;
        _lastClickTime = currentTime;
        e.Handled = true;
    }

    public void OnPointerMoved(IPointerEventArgs e)
    {
        if (selectionHandler.IsSelecting || IsDoubleClickDrag || IsTripleClickDrag)
        {
            var position = editorCommands.GetPositionFromPoint(e.GetPosition());
            selectionHandler.UpdateSelectionDuringDrag(position, IsDoubleClickDrag, IsTripleClickDrag);

            PublishCursorPositionChanged(position);
            e.Handled = true;
        }
    }

    public void OnPointerReleased(IPointerReleasedEventArgs e)
    {
        selectionHandler.EndSelection();
        IsTripleClickDrag = false;
        IsDoubleClickDrag = false;

        eventAggregator.Publish(new IsSelectingChangedEventArgs(false));
    
        e.Handled = true;
    }

    public async Task OnKeyDown(IKeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                cursorManager.MoveCursorLeft(e.IsShiftPressed);
                break;
            case Key.Right:
                cursorManager.MoveCursorRight(e.IsShiftPressed);
                break;
            case Key.Up:
                cursorManager.MoveCursorUp(e.IsShiftPressed);
                break;
            case Key.Down:
                cursorManager.MoveCursorDown(e.IsShiftPressed);
                break;
            case Key.Home:
                cursorManager.MoveCursorToLineStart(e.IsShiftPressed);
                break;
            case Key.End:
                cursorManager.MoveCursorToLineEnd(e.IsShiftPressed);
                break;
            case Key.Back:
                editorCommands.HandleBackspace();
                break;
            case Key.Delete:
                editorCommands.HandleDelete();
                break;
            case Key.Enter:
                editorCommands.InsertNewLine();
                break;
        }

        PublishCursorPositionChanged(cursorManager.Position);

        if (e.IsControlPressed)
            await HandleControlKeyCombo(e);

        e.Handled = true;
    }

    public void OnTextInput(ITextInputEventArgs e)
    {
        logger.LogDebug($"Text input: {e.Text}");
        if (!string.IsNullOrEmpty(e.Text))
        {
            var oldPosition = cursorManager.Position;
            editorCommands.InsertText(cursorManager.Position, e.Text);
            eventAggregator.Publish(new TextChangedEventArgs(oldPosition, e.Text, 0));
            PublishCursorPositionChanged(cursorManager.Position);
            e.Handled = true;
        }
    }

    private void PublishCursorPositionChanged(int newPosition)
    {
        var lineStarts = textBuffer.GetLineStarts();
        var lastLineLength = textBuffer.GetLineLength(textBuffer.LineCount - 1);
        eventAggregator.Publish(new CursorPositionChangedEventArgs(newPosition, lineStarts, lastLineLength));
    }

    private bool IsTripleClick(DateTime currentTime, IPoint? currentPosition)
    {
        return (currentTime - _lastClickTime).TotalMilliseconds <= TripleClickTimeThreshold &&
               DistanceBetweenPoints(currentPosition, _lastClickPosition) <= DoubleClickDistanceThreshold &&
               (currentTime - _lastClickTime).TotalMilliseconds > DoubleClickTimeThreshold;
    }

    private bool IsDoubleClick(DateTime currentTime, IPoint? currentPosition)
    {
        return (currentTime - _lastClickTime).TotalMilliseconds <= DoubleClickTimeThreshold &&
               DistanceBetweenPoints(currentPosition, _lastClickPosition) <= DoubleClickDistanceThreshold;
    }

    private async Task HandleControlKeyCombo(IKeyEventArgs e)
    {
        Console.WriteLine($"Control key combo: {e.Key}");
        switch (e.Key)
        {
            case Key.C:
                await editorCommands.CopyText();
                break;
            case Key.V:
                await editorCommands.PasteText();
                break;
            case Key.X:
                await editorCommands.CutText();
                break;
            case Key.A:
                selectionHandler.SelectAll();
                break;
            case Key.Z:
                if (e.IsShiftPressed)
                    editorCommands.Redo();
                else
                    editorCommands.Undo();
                break;
        }
    }

    private double DistanceBetweenPoints(IPoint? p1, IPoint? p2)
    {
        if (p1 != null && p2 != null) return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        throw new ArgumentNullException(nameof(p1));
    }

    public void Dispose()
    {
        // TODO dispose of resources
    }
}
