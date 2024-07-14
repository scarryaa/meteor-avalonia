namespace meteor.Interfaces;

public interface IUndoRedoManager<T>
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    void AddState(T state, string description);
    (T state, string description) Undo();
    (T state, string description) Redo();
    void Clear();
}