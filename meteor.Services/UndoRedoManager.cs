using meteor.Core.Interfaces;
using meteor.Core.Models;

namespace meteor.Services;

public class UndoRedoManager<T> : IUndoRedoManager<T>
{
    private readonly CircularBuffer<UndoRedoAction<T>> _undoBuffer;
    private readonly CircularBuffer<UndoRedoAction<T>> _redoBuffer;

    public bool CanUndo => _undoBuffer.Count > 0;
    public bool CanRedo => _redoBuffer.Count > 0;

    public event EventHandler? StateChanged;
    public T CurrentState { get; private set; }

    public UndoRedoManager(T initialState, int maxUndoLevels = 100)
    {
        _undoBuffer = new CircularBuffer<UndoRedoAction<T>>(maxUndoLevels);
        _redoBuffer = new CircularBuffer<UndoRedoAction<T>>(maxUndoLevels);

        if (initialState == null) throw new ArgumentNullException(nameof(initialState), "initialState cannot be null");

        CurrentState = initialState;
    }

    public void AddState(T newState, string description)
    {
        if (newState == null) throw new ArgumentNullException(nameof(newState), "Attempted to add a null state.");

        _undoBuffer.Add(new UndoRedoAction<T>(CurrentState, newState, description));
        _redoBuffer.Clear();
        CurrentState = newState;
        OnStateChanged();
    }

    public (T state, string description) Undo()
    {
        if (!CanUndo) throw new InvalidOperationException("Cannot undo: No actions to undo.");

        var action = _undoBuffer.Remove();
        _redoBuffer.Add(action);
        CurrentState = action.OldState;
        OnStateChanged();

        return (CurrentState, action.Description);
    }

    public (T state, string description) Redo()
    {
        if (!CanRedo) throw new InvalidOperationException("Cannot redo: No actions to redo.");

        var action = _redoBuffer.Remove();
        _undoBuffer.Add(action);
        CurrentState = action.NewState;
        OnStateChanged();

        return (CurrentState, action.Description);
    }

    public void Clear()
    {
        _undoBuffer.Clear();
        _redoBuffer.Clear();
        OnStateChanged();
    }

    public IEnumerable<string> GetUndoDescriptions()
    {
        foreach (var action in _undoBuffer)
            yield return action.Description;
    }

    public IEnumerable<string> GetRedoDescriptions()
    {
        foreach (var action in _redoBuffer)
            yield return action.Description;
    }

    protected virtual void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private class UndoRedoAction<TState>(TState oldState, TState newState, string description)
    {
        public TState OldState { get; } =
            oldState ?? throw new ArgumentNullException(nameof(oldState), "oldState cannot be null");

        public TState NewState { get; } =
            newState ?? throw new ArgumentNullException(nameof(newState), "newState cannot be null");

        public string Description { get; } = description;
    }
}