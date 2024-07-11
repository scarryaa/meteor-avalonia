using System;
using System.Collections.Generic;
using meteor.Interfaces;
using Microsoft.Extensions.Logging;

namespace meteor.Models;

public class UndoRedoManager<T> : IUndoRedoManager<T>
{
    private readonly CircularBuffer<UndoRedoAction<T>> _undoBuffer;
    private readonly CircularBuffer<UndoRedoAction<T>> _redoBuffer;
    private readonly ILogger<UndoRedoManager<T>> _logger;
    
    public bool CanUndo => _undoBuffer.Count > 0;
    public bool CanRedo => _redoBuffer.Count > 0;

    public event EventHandler? StateChanged;
    public T CurrentState { get; private set; }

    public UndoRedoManager(T initialState, int maxUndoLevels = 100)
    {
        _logger = ServiceLocator.GetService<ILogger<UndoRedoManager<T>>>();
        _undoBuffer = new CircularBuffer<UndoRedoAction<T>>(maxUndoLevels);
        _redoBuffer = new CircularBuffer<UndoRedoAction<T>>(maxUndoLevels);

        if (initialState == null) throw new ArgumentNullException(nameof(initialState), "initialState cannot be null");

        CurrentState = initialState;
        // Initial state should not be added to the undo buffer
    }

    public void AddState(T newState, string description)
    {
        if (newState == null)
        {
            _logger.LogError("Attempted to add a null state.");
            return;
        }

        _undoBuffer.Add(new UndoRedoAction<T>(CurrentState, newState, description));
        _redoBuffer.Clear(); // Clear redo stack when a new action is added
        CurrentState = newState;
        OnStateChanged();
    }

    public (T state, string description) Undo()
    {
        if (!CanUndo) throw new InvalidOperationException("Cannot undo: No actions to undo.");

        var action = _undoBuffer.Remove();
        if (action == null || action.OldState == null)
        {
            _logger.LogError("Action or action.OldState is null during undo.");
            return (default, "Error: action or action.OldState is null during undo.");
        }

        _redoBuffer.Add(action); // Add the undone action to the redo buffer
        CurrentState = action.OldState; // Update current state to the old state
        OnStateChanged();

        return (CurrentState, action.Description);
    }

    public (T state, string description) Redo()
    {
        if (!CanRedo) throw new InvalidOperationException("Cannot redo: No actions to redo.");

        var action = _redoBuffer.Remove();
        if (action == null || action.NewState == null)
        {
            _logger.LogError("Action or action.NewState is null during redo.");
            return (default, "Action or action.NewState is null during redo.")!;
        }

        _logger.LogInformation($"Redoing state: {action.NewState}");
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
            if (oldState == null) throw new ArgumentNullException(nameof(oldState), "oldState cannot be null");
            if (newState == null) throw new ArgumentNullException(nameof(newState), "newState cannot be null");

            OldState = oldState;
            NewState = newState;
            Description = description;
        }
    }
}