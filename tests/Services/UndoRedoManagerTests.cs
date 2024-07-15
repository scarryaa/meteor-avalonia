using meteor.Models;
using meteor.Services;

namespace tests.Services;

public class UndoRedoManagerTests
{
    [Fact]
    public void AddState_ShouldAddNewState()
    {
        var initialState = new TextState("Initial", 0);
        var manager = new UndoRedoManager<TextState>(initialState);

        var newState = new TextState("New", 1);
        manager.AddState(newState, "Added new state");

        Assert.Equal(newState, manager.CurrentState);
        Assert.Single(manager.GetUndoDescriptions());
    }

    [Fact]
    public void Undo_ShouldRevertToPreviousState()
    {
        var initialState = new TextState("Initial", 0);
        var manager = new UndoRedoManager<TextState>(initialState);

        var newState = new TextState("New", 1);
        manager.AddState(newState, "Added new state");

        var (state, description) = manager.Undo();

        Assert.Equal(initialState.Text, state.Text);
        Assert.Equal(initialState.CursorPosition, state.CursorPosition);
        Assert.Equal("Added new state", description);
        Assert.Equal(initialState, manager.CurrentState);
    }

    [Fact]
    public void Redo_ShouldReapplyNextState()
    {
        var initialState = new TextState("Initial", 0);
        var manager = new UndoRedoManager<TextState>(initialState);

        var newState = new TextState("New", 1);
        manager.AddState(newState, "Added new state");

        manager.Undo();
        var (state, description) = manager.Redo();

        Assert.Equal(newState.Text, state.Text);
        Assert.Equal(newState.CursorPosition, state.CursorPosition);
        Assert.Equal("Added new state", description);
        Assert.Equal(newState, manager.CurrentState);
    }

    [Fact]
    public void Undo_ShouldThrowExceptionWhenNoActionsToUndo()
    {
        var initialState = new TextState("Initial", 0);
        var manager = new UndoRedoManager<TextState>(initialState);

        Assert.Throws<InvalidOperationException>(() => manager.Undo());
    }

    [Fact]
    public void Redo_ShouldThrowExceptionWhenNoActionsToRedo()
    {
        var initialState = new TextState("Initial", 0);
        var manager = new UndoRedoManager<TextState>(initialState);

        Assert.Throws<InvalidOperationException>(() => manager.Redo());
    }

    [Fact]
    public void AddState_ShouldClearRedoBuffer()
    {
        var initialState = new TextState("Initial", 0);
        var manager = new UndoRedoManager<TextState>(initialState);

        var newState1 = new TextState("New1", 1);
        var newState2 = new TextState("New2", 2);

        manager.AddState(newState1, "Added new state 1");
        manager.Undo();
        manager.AddState(newState2, "Added new state 2");

        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void Clear_ShouldEmptyUndoAndRedoBuffers()
    {
        var initialState = new TextState("Initial", 0);
        var manager = new UndoRedoManager<TextState>(initialState);

        var newState = new TextState("New", 1);
        manager.AddState(newState, "Added new state");

        manager.Clear();

        Assert.False(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void Undo_ShouldNotRepeatOperationsWhenAllOperationsUndone()
    {
        var initialState = new TextState("Initial", 0);
        var manager = new UndoRedoManager<TextState>(initialState);

        var newState1 = new TextState("New1", 1);
        var newState2 = new TextState("New2", 2);

        manager.AddState(newState1, "Added new state 1");
        manager.AddState(newState2, "Added new state 2");

        manager.Undo();
        manager.Undo();

        // Now the buffer should be at the initial state, next undo should throw an exception
        Assert.Throws<InvalidOperationException>(() => manager.Undo());

        // Check that no states are in the undo buffer after all operations have been undone
        Assert.False(manager.CanUndo);
    }
}