using System.Windows.Input;
using Avalonia.Controls;
using meteor.rendering.Services;
using ReactiveUI;

namespace meteor.avalonia.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private CodeEditorViewModel _codeEditor;

    public MainWindowViewModel(TopLevel topLevel)
    {
        var clipboardService = new AvaloniaClipboard(topLevel);
        CodeEditor = new CodeEditorViewModel(clipboardService);
        InitializeCommands();
    }

    public CodeEditorViewModel CodeEditor
    {
        get => _codeEditor;
        set => this.RaiseAndSetIfChanged(ref _codeEditor, value);
    }

    public ICommand OpenCommand { get; private set; }
    public ICommand SaveCommand { get; private set; }
    public ICommand ExitCommand { get; private set; }
    public ICommand UndoCommand { get; private set; }
    public ICommand RedoCommand { get; private set; }
    public ICommand CutCommand { get; private set; }
    public ICommand CopyCommand { get; private set; }
    public ICommand PasteCommand { get; private set; }

    private void InitializeCommands()
    {
        OpenCommand = ReactiveCommand.Create(Open);
        SaveCommand = ReactiveCommand.Create(Save);
        ExitCommand = ReactiveCommand.Create(Exit);
        UndoCommand = ReactiveCommand.Create(Undo);
        RedoCommand = ReactiveCommand.Create(Redo);
        CutCommand = ReactiveCommand.Create(Cut);
        CopyCommand = ReactiveCommand.Create(Copy);
        PasteCommand = ReactiveCommand.Create(Paste);
    }

    private void Open()
    {
        // Implement file open logic
    }

    private void Save()
    {
        // Implement file save logic
    }

    private void Exit()
    {
        // Implement application exit logic
    }

    private void Undo()
    {
        CodeEditor.Undo();
    }

    private void Redo()
    {
        CodeEditor.Redo();
    }

    private void Cut()
    {
        CodeEditor.Cut();
    }

    private void Copy()
    {
        CodeEditor.Copy();
    }

    private void Paste()
    {
        CodeEditor.Paste();
    }
}