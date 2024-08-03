using meteor.Core.Models;

namespace meteor.Core.Services
{
    public class UndoRedoManager
    {
        private readonly Stack<TextChange> _undoStack = new Stack<TextChange>();
        private readonly Stack<TextChange> _redoStack = new Stack<TextChange>();

        public void RecordChange(TextChange change)
        {
            _undoStack.Push(change);
            _redoStack.Clear();
        }

        public TextChange? Undo()
        {
            if (_undoStack.Count > 0)
            {
                var change = _undoStack.Pop();
                _redoStack.Push(change);
                return change;
            }
            return null;
        }

        public TextChange? Redo()
        {
            if (_redoStack.Count > 0)
            {
                var change = _redoStack.Pop();
                _undoStack.Push(change);
                return change;
            }
            return null;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}