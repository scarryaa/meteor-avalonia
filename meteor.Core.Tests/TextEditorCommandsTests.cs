using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Contexts;
using meteor.Core.Interfaces.Events;
using meteor.Core.Interfaces.Rendering;
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

    [Fact]
    public void HandleBackspace_WithoutSelection_ShouldDeletePreviousCharacter()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(false);
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);

        // Act
        _commands.HandleBackspace();

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(4, 1), Times.Once);
        _mockCursorManager.Verify(cm => cm.MoveCursorLeft(false), Times.Once);
    }

    [Fact]
    public void HandleDelete_WithSelection_ShouldDeleteSelectedText()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(true);
        _mockSelectionHandler.Setup(sh => sh.SelectionStart).Returns(5);
        _mockSelectionHandler.Setup(sh => sh.SelectionEnd).Returns(10);

        // Act
        _commands.HandleDelete();

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(5, 5), Times.Once);
        _mockCursorManager.Verify(cm => cm.SetPosition(5), Times.Once);
        _mockSelectionHandler.Verify(sh => sh.ClearSelection(), Times.Once);
    }

    [Fact]
    public void HandleDelete_WithoutSelection_ShouldDeleteNextCharacter()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(false);
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);

        // Act
        _commands.HandleDelete();

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(5, 1), Times.Once);
    }

    [Fact]
    public void InsertNewLine_ShouldInsertNewLineCharacter()
    {
        // Arrange
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);

        // Act
        _commands.InsertNewLine();

        // Assert
        _mockTextBuffer.Verify(tb => tb.InsertText(5, Environment.NewLine), Times.Once);
        _mockCursorManager.Verify(cm => cm.SetPosition(5 + Environment.NewLine.Length), Times.Once);
    }

    [Fact]
    public async Task CutText_WithSelection_ShouldCopyAndDeleteSelectedText()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(true);
        _mockSelectionHandler.Setup(sh => sh.SelectionStart).Returns(5);
        _mockSelectionHandler.Setup(sh => sh.SelectionEnd).Returns(10);
        _mockTextBuffer.Setup(tb => tb.GetText(5, 5)).Returns("Hello");

        // Act
        await _commands.CutText();

        // Assert
        _mockClipboardService.Verify(cs => cs.SetTextAsync("Hello"), Times.Once);
        _mockTextBuffer.Verify(tb => tb.DeleteText(5, 5), Times.Once);
        _mockCursorManager.Verify(cm => cm.SetPosition(5), Times.Once);
        _mockSelectionHandler.Verify(sh => sh.ClearSelection(), Times.Once);
    }

    [Fact]
    public void GetPositionFromPoint_ShouldReturnCorrectPosition()
    {
        // Arrange
        var point = Mock.Of<IPoint>(p => p.X == 50 && p.Y == 20);
        _mockContext.Setup(c => c.VerticalOffset).Returns(0);
        _mockContext.Setup(c => c.LineHeight).Returns(20);
        _mockContext.Setup(c => c.FontSize).Returns(12);
        _mockContext.Setup(c => c.FontFamily.Name).Returns("Arial");
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(5);
        _mockTextBuffer.Setup(tb => tb.GetLineText(1)).Returns("Sample Text");
        _mockTextBuffer.Setup(tb => tb.GetLineLength(0)).Returns(10);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(50);
        _mockTextMeasurer.Setup(tm => tm.MeasureWidth("Sample Text", 12, "Arial")).Returns(100);

        // Act
        var result = _commands.GetPositionFromPoint(point);

        // Assert
        Assert.Equal(6, result);
    }
}