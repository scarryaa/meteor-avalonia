using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class EditorViewModelServiceContainer
{
    public ITextBufferService TextBufferService { get; }
    public ITabService TabService { get; }
    public ISyntaxHighlighter SyntaxHighlighter { get; }
    public ISelectionService SelectionService { get; }
    public IInputService InputService { get; }
    public ICursorService CursorService { get; }
    public IEditorSizeCalculator SizeCalculator { get; }

    public EditorViewModelServiceContainer(
        ITextBufferService textBufferService,
        ITabService tabService,
        ISyntaxHighlighter syntaxHighlighter,
        ISelectionService selectionService,
        IInputService inputService,
        ICursorService cursorService,
        IEditorSizeCalculator sizeCalculator)
    {
        TextBufferService = textBufferService;
        TabService = tabService;
        SyntaxHighlighter = syntaxHighlighter;
        SelectionService = selectionService;
        InputService = inputService;
        CursorService = cursorService;
        SizeCalculator = sizeCalculator;
    }
}