using meteor.Core.Interfaces;
using Moq;

namespace meteor.Services.Tests;

public class CursorManagerTests
{
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly Mock<ISelectionHandler> _mockSelectionHandler;
    private readonly Mock<IWordBoundaryService> _mockWordBoundaryService;
    private readonly CursorManager _cursorManager;

    public CursorManagerTests()
    {
        _mockTextBuffer = new Mock<ITextBuffer>();
        _mockSelectionHandler = new Mock<ISelectionHandler>();
        _mockWordBoundaryService = new Mock<IWordBoundaryService>();
        _cursorManager = new CursorManager(_mockTextBuffer.Object, _mockSelectionHandler.Object,
            _mockWordBoundaryService.Object);

        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
    }

    [Fact]
    public void SetPosition_ShouldClampToValidRange()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);

        // Act & Assert
        _cursorManager.SetPosition(-1);
        Assert.Equal(0, _cursorManager.Position);

        _cursorManager.SetPosition(5);
        Assert.Equal(5, _cursorManager.Position);

        _cursorManager.SetPosition(15);
        Assert.Equal(10, _cursorManager.Position);
    }

    [Fact]
    public void MoveCursorLeft_WithoutSelection_ShouldDecrementPosition()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(false);
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveCursorLeft(false);

        // Assert
        Assert.Equal(4, _cursorManager.Position);
    }

    [Fact]
    public void MoveCursorLeft_WithSelection_ShouldMoveToSelectionStart()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(true);
        _mockSelectionHandler.Setup(sh => sh.SelectionStart).Returns(3);
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveCursorLeft(false);

        // Assert
        Assert.Equal(3, _cursorManager.Position);
        _mockSelectionHandler.Verify(sh => sh.ClearSelection(), Times.Once);
    }

    [Fact]
    public void MoveCursorRight_WithoutSelection_ShouldIncrementPosition()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveCursorRight(false);

        // Assert
        Assert.Equal(6, _cursorManager.Position);
    }

    [Fact]
    public void MoveCursorRight_WithSelection_ShouldMoveToSelectionEnd()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(true);
        _mockSelectionHandler.Setup(sh => sh.SelectionEnd).Returns(7);
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveCursorRight(false);

        // Assert
        Assert.Equal(7, _cursorManager.Position);
        _mockSelectionHandler.Verify(sh => sh.ClearSelection(), Times.Once);
    }

    [Fact]
    public void MoveCursorUp_ShouldMoveToCorrectPosition()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(5)).Returns(1);
        _mockTextBuffer.Setup(tb => tb.GetLineStartPosition(1)).Returns(3);
        _mockTextBuffer.Setup(tb => tb.GetLineStartPosition(0)).Returns(0);
        _mockTextBuffer.Setup(tb => tb.GetLineLength(0)).Returns(3);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveCursorUp(false);

        // Assert
        Assert.Equal(2, _cursorManager.Position);
    }

    [Fact]
    public void MoveCursorDown_ShouldMoveToCorrectPosition()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(2)).Returns(0);
        _mockTextBuffer.Setup(tb => tb.GetLineStartPosition(0)).Returns(0);
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(2);
        _mockTextBuffer.Setup(tb => tb.GetLineStartPosition(1)).Returns(3);
        _mockTextBuffer.Setup(tb => tb.GetLineLength(1)).Returns(4);
        _cursorManager.SetPosition(2);

        // Act
        _cursorManager.MoveCursorDown(false);

        // Assert
        Assert.Equal(5, _cursorManager.Position);
    }

    [Fact]
    public void MoveCursorToLineStart_ShouldMoveToLineStart()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(5)).Returns(1);
        _mockTextBuffer.Setup(tb => tb.GetLineStartPosition(1)).Returns(3);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveCursorToLineStart(false);

        // Assert
        Assert.Equal(3, _cursorManager.Position);
    }

    [Fact]
    public void MoveCursorToLineEnd_ShouldMoveToLineEnd()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(3)).Returns(1);
        _mockTextBuffer.Setup(tb => tb.GetLineEndPosition(1)).Returns(7);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _cursorManager.SetPosition(3);

        // Act
        _cursorManager.MoveCursorToLineEnd(false);

        // Assert
        Assert.Equal(7, _cursorManager.Position);
    }

    [Fact]
    public void MoveWordLeft_ShouldMoveToPreviousWordBoundary()
    {
        // Arrange
        _mockWordBoundaryService.Setup(wbs => wbs.GetPreviousWordBoundary(_mockTextBuffer.Object, 5)).Returns(2);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveWordLeft(false);

        // Assert
        Assert.Equal(2, _cursorManager.Position);
    }

    [Fact]
    public void MoveWordRight_ShouldMoveToNextWordBoundary()
    {
        // Arrange
        _mockWordBoundaryService.Setup(wbs => wbs.GetNextWordBoundary(_mockTextBuffer.Object, 5)).Returns(8);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveWordRight(false);

        // Assert
        Assert.Equal(8, _cursorManager.Position);
    }


    [Fact]
    public void MoveToDocumentStart_ShouldMoveToBeginning()
    {
        // Arrange
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveToDocumentStart(false);

        // Assert
        Assert.Equal(0, _cursorManager.Position);
    }

    [Fact]
    public void MoveToDocumentEnd_ShouldMoveToEnd()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveToDocumentEnd(false);

        // Assert
        Assert.Equal(10, _cursorManager.Position);
    }

    [Fact]
    public void MoveCursor_WithShiftPressed_ShouldUpdateSelection()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.IsSelecting).Returns(false);
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(It.IsAny<int>())).Returns(0);
        _mockTextBuffer.Setup(tb => tb.GetLineStartPosition(0)).Returns(0);
        _cursorManager.SetPosition(5);

        // Act
        _cursorManager.MoveCursorRight(true);

        // Assert
        _mockSelectionHandler.Verify(sh => sh.StartSelection(It.IsAny<int>()), Times.Once);
        _mockSelectionHandler.Verify(sh => sh.UpdateSelectionDuringDrag(6, false, false), Times.Once);

        // Additional verification to ensure StartSelection is called before UpdateSelectionDuringDrag
        var sequence = new MockSequence();
        _mockSelectionHandler.InSequence(sequence).Setup(sh => sh.StartSelection(It.IsAny<int>()));
        _mockSelectionHandler.InSequence(sequence).Setup(sh =>
            sh.UpdateSelectionDuringDrag(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()));
    }

    [Fact]
    public void MoveCursor_WithoutShiftPressed_ShouldClearSelection()
    {
        // Arrange
        _cursorManager.SetPosition(5);
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(true);

        // Act
        _cursorManager.MoveCursorRight(false);

        // Assert
        _mockSelectionHandler.Verify(sh => sh.ClearSelection(), Times.Once);
    }

    [Fact]
    public void MoveCursorLeft_AtDocumentStart_ShouldNotMove()
    {
        // Arrange
        _cursorManager.SetPosition(0);

        // Act
        _cursorManager.MoveCursorLeft(false);

        // Assert
        Assert.Equal(0, _cursorManager.Position);
    }

    [Fact]
    public void MoveCursorRight_AtDocumentEnd_ShouldNotMove()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _cursorManager.SetPosition(10);

        // Act
        _cursorManager.MoveCursorRight(false);

        // Assert
        Assert.Equal(10, _cursorManager.Position);
    }

    [Fact]
    public void MoveCursorUp_AtDocumentStart_ShouldNotMove()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(0)).Returns(0);
        _cursorManager.SetPosition(0);

        // Act
        _cursorManager.MoveCursorUp(false);

        // Assert
        Assert.Equal(0, _cursorManager.Position);
    }

    [Fact]
    public void MoveCursorDown_AtDocumentEnd_ShouldNotMove()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(10)).Returns(1);
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(2);
        _cursorManager.SetPosition(10);

        // Act
        _cursorManager.MoveCursorDown(false);

        // Assert
        Assert.Equal(10, _cursorManager.Position);
    }

    [Fact]
    public void MoveWordLeft_AtDocumentStart_ShouldNotMove()
    {
        // Arrange
        _mockWordBoundaryService.Setup(wbs => wbs.GetPreviousWordBoundary(_mockTextBuffer.Object, 0)).Returns(0);
        _cursorManager.SetPosition(0);

        // Act
        _cursorManager.MoveWordLeft(false);

        // Assert
        Assert.Equal(0, _cursorManager.Position);
    }

    [Fact]
    public void MoveWordRight_AtDocumentEnd_ShouldNotMove()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _mockWordBoundaryService.Setup(wbs => wbs.GetNextWordBoundary(_mockTextBuffer.Object, 10)).Returns(10);
        _cursorManager.SetPosition(10);

        // Act
        _cursorManager.MoveWordRight(false);

        // Assert
        Assert.Equal(10, _cursorManager.Position);
    }
}