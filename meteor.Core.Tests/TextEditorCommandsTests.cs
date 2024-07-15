using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Contexts;
using meteor.Core.Interfaces.Events;
using meteor.Core.Models.Commands;
using Moq;

namespace meteor.Core.Tests;

public class TextEditorCommandsTests
{
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly Mock<ICursorManager> _mockCursorManager;
    private readonly Mock<ISelectionHandler> _mockSelectionHandler;
    private readonly Mock<IClipboardService> _mockClipboardService;
    private readonly Mock<IUndoRedoManager<ITextBuffer>> _mockUndoRedoManager;
    private readonly Mock<ITextEditorContext> _mockContext;
    private readonly Mock<ITextMeasurer> _mockTextMeasurer;
    private readonly TextEditorCommands _commands;
    private readonly Mock<IEventAggregator> _mockEventAggregator;

    public TextEditorCommandsTests()
    {
        _mockTextBuffer = new Mock<ITextBuffer>();
        _mockCursorManager = new Mock<ICursorManager>();
        _mockSelectionHandler = new Mock<ISelectionHandler>();
        _mockClipboardService = new Mock<IClipboardService>();
        _mockUndoRedoManager = new Mock<IUndoRedoManager<ITextBuffer>>();
        _mockContext = new Mock<ITextEditorContext>();
        _mockTextMeasurer = new Mock<ITextMeasurer>();
        _mockEventAggregator = new Mock<IEventAggregator>();

        _commands = new TextEditorCommands(
            _mockTextBuffer.Object,
            _mockCursorManager.Object,
            _mockSelectionHandler.Object,
            _mockClipboardService.Object,
            _mockUndoRedoManager.Object,
            _mockContext.Object,
            _mockTextMeasurer.Object,
            _mockEventAggregator.Object
        );
    }

    [Fact]
    public void InsertText_ShouldInsertTextAndMoveCursor()
    {
        // Arrange
        var position = 5;
        var text = "Hello";

        // Act
        _commands.InsertText(position, text);

        // Assert
        _mockTextBuffer.Verify(tb => tb.InsertText(position, text), Times.Once);
        _mockCursorManager.Verify(cm => cm.SetPosition(position + text.Length), Times.Once);
    }

    [Fact]
    public void HandleBackspace_WithSelection_ShouldDeleteSelectedText()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(true);

        // Act
        _commands.HandleBackspace();

        // Assert
        _mockSelectionHandler.Verify(sh => sh.ClearSelection(), Times.Once);
    }

    [Fact]
    public async Task CopyText_WithSelection_ShouldCopyToClipboard()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(true);
        _mockSelectionHandler.Setup(sh => sh.SelectionStart).Returns(0);
        _mockSelectionHandler.Setup(sh => sh.SelectionEnd).Returns(5);
        _mockTextBuffer.Setup(tb => tb.GetText(0, 5)).Returns("Hello");

        // Act
        await _commands.CopyText();

        // Assert
        _mockClipboardService.Verify(cs => cs.SetTextAsync("Hello"), Times.Once);
    }

    [Fact]
    public async Task PasteText_ShouldInsertClipboardText()
    {
        // Arrange
        _mockClipboardService.Setup(cs => cs.GetTextAsync()).ReturnsAsync("Pasted");
        _mockCursorManager.Setup(cm => cm.Position).Returns(0);

        // Act
        await _commands.PasteText();

        // Assert
        _mockTextBuffer.Verify(tb => tb.InsertText(0, "Pasted"), Times.Once);
    }

    [Fact]
    public void Undo_ShouldCallUndoRedoManager()
    {
        // Act
        _commands.Undo();

        // Assert
        _mockUndoRedoManager.Verify(ur => ur.Undo(), Times.Once);
    }

    [Fact]
    public void Redo_ShouldCallUndoRedoManager()
    {
        // Act
        _commands.Redo();

        // Assert
        _mockUndoRedoManager.Verify(ur => ur.Redo(), Times.Once);
    }
}