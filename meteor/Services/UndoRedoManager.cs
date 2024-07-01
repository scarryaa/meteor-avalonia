using System;
using System.Collections.Generic;
using meteor.Models;

public class UndoRedoManager<T> where T : TextState
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
        AddState(initialState, "Initial state"); // Store initial state
    }

    public void AddState(T newState, string description)
    {
        _undoBuffer.Add(new UndoRedoAction<T>(CurrentState, newState, description));
        _redoBuffer.Clear(); // Clear redo stack when a new action is added
        CurrentState = newState;
        OnStateChanged();
    }

    public (T State, string Description) Undo()
    {
        if (!CanUndo) throw new InvalidOperationException("Cannot undo: No actions to undo.");

        var action = _undoBuffer.Remove();
        _redoBuffer.Add(action); // Add the undone action to the redo buffer
        CurrentState = action.OldState; // Update current state to the old state
        OnStateChanged();

        return (CurrentState, action.Description);
    }

    public (T State, string Description) Redo()
    {
        if (!CanRedo) throw new InvalidOperationException("Cannot redo: No actions to redo.");

        var action = _redoBuffer.Remove();
        _undoBuffer.Add(action); // Add the redone action to the undo buffer
        CurrentState = action.NewState; // Update current state to the new state
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

    private class UndoRedoAction<TState>
    {
        public TState OldState { get; }
        public TState NewState { get; }
        public string Description { get; }

        public UndoRedoAction(TState oldState, TState newState, string description)
        {
            OldState = oldState;
            NewState = newState;
            Description = description;
        }
    }
}
