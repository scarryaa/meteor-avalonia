using System;
using System.Collections.Generic;
using System.Linq;
using meteor.Models;

public class UndoRedoManager<T> where T : TextState
{
    private readonly Stack<UndoRedoAction<T>> _undoStack = new();
    private readonly Stack<UndoRedoAction<T>> _redoStack = new();
    private readonly int _maxUndoLevels;
    private T _currentState;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? StateChanged;

    public UndoRedoManager(T initialState, int maxUndoLevels = 100)
    {
        _currentState = initialState;
        _maxUndoLevels = maxUndoLevels;
    }

    public void AddState(T newState, string description)
    {
        if (!EqualityComparer<T>.Default.Equals(_currentState, newState))
        {
            var action = new UndoRedoAction<T>(_currentState, newState, description);
            _undoStack.Push(action);
            _redoStack.Clear();

            if (_undoStack.Count > _maxUndoLevels)
            {
                var oldestAction = new Stack<UndoRedoAction<T>>(_undoStack);
                oldestAction.Pop();
                _undoStack.Clear();
                while (oldestAction.Count > 0) _undoStack.Push(oldestAction.Pop());
            }

            _currentState = newState;
            OnStateChanged();
        }
    }

    public (T State, string Description) Undo()
    {
        if (!CanUndo) throw new InvalidOperationException("Cannot undo: No actions to undo.");

        var action = _undoStack.Pop();
        _redoStack.Push(action);
        _currentState = action.OldState;
        OnStateChanged();

        return (_currentState, action.Description);
    }

    public (T State, string Description) Redo()
    {
        if (!CanRedo) throw new InvalidOperationException("Cannot redo: No actions to redo.");

        var action = _redoStack.Pop();
        _undoStack.Push(action);
        _currentState = action.NewState;
        OnStateChanged();

        return (_currentState, action.Description);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        OnStateChanged();
    }

    public IEnumerable<string> GetUndoDescriptions()
    {
        return _undoStack.Select(action => action.Description);
    }

    public IEnumerable<string> GetRedoDescriptions()
    {
        return _redoStack.Select(action => action.Description);
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