namespace meteor.core.Services;

public class UndoRedoManager
{
    private readonly Stack<Action> _undoStack = new();
    private readonly Stack<Action> _redoStack = new();

    public void Execute(Action action, Action undoAction)
    {
        action();
        _undoStack.Push(undoAction);
        _redoStack.Clear();
    }

    public void Undo()
    {
        if (_undoStack.Count > 0)
        {
            var undoAction = _undoStack.Pop();
            undoAction();
            _redoStack.Push(undoAction);
        }
    }

    public void Redo()
    {
        if (_redoStack.Count > 0)
        {
            var redoAction = _redoStack.Pop();
            redoAction();
            _undoStack.Push(redoAction);
        }
    }
}