namespace meteor.Core.Interfaces;

public interface IUndoRedoManager<T>
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    T CurrentState { get; }
    event EventHandler? StateChanged;

    void AddState(T newState, string description);
    (T state, string description) Undo();
    (T state, string description) Redo();
    void Clear();
    IEnumerable<string> GetUndoDescriptions();
    IEnumerable<string> GetRedoDescriptions();
}