using meteor.Application.Interfaces;
using meteor.Application.Services;
using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.Events;
using Moq;
using Xunit.Abstractions;

namespace meteor.UnitTests.Application.Services;

public class InputServiceTests
{
    private readonly Mock<ITextBufferService> _textBufferServiceMock;
    private readonly Mock<ICursorService> _cursorServiceMock;
    private readonly Mock<ITextAnalysisService> _textAnalysisServiceMock;
    private readonly Mock<ISelectionService> _selectionServiceMock;
    private readonly InputService _inputService;
    private readonly ITestOutputHelper _output;

    public InputServiceTests(ITestOutputHelper output)
    {
        _textBufferServiceMock = new Mock<ITextBufferService>();
        _cursorServiceMock = new Mock<ICursorService>();
        _textAnalysisServiceMock = new Mock<ITextAnalysisService>();
        _selectionServiceMock = new Mock<ISelectionService>();
        _inputService = new InputService(_textBufferServiceMock.Object, _cursorServiceMock.Object,
            _textAnalysisServiceMock.Object, _selectionServiceMock.Object);
        _output = output;
    }

    [Fact]
    public void InsertText_CallsInsertAndSetsCursorPosition()
    {
        // Arrange
        var cursorPosition = 5;
        var text = "world";
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(cursorPosition);
        _textBufferServiceMock.Setup(t => t.Length).Returns(10);

        // Act
        _inputService.InsertText(text);

        // Assert
        _textBufferServiceMock.Verify(t => t.Insert(cursorPosition, text), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(cursorPosition + text.Length), Times.Once);
    }

    [Fact]
    public void DeleteText_CallsDelete()
    {
        // Arrange
        var index = 5;
        var length = 3;

        // Act
        _inputService.DeleteText(index, length);

        // Assert
        _textBufferServiceMock.Verify(t => t.Delete(index, length), Times.Once);
    }

    [Fact]
    public void HandleKeyDown_Backspace_CallsDeleteAndUpdateCursorPosition()
    {
        // Arrange
        var cursorPosition = 5;
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(cursorPosition);
        _textBufferServiceMock.Setup(t => t.Length).Returns(10);

        // Act
        _inputService.HandleKeyDown(Key.Backspace);

        // Assert
        _textBufferServiceMock.Verify(t => t.Delete(cursorPosition - 1, 1), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(cursorPosition - 1), Times.Once);
    }

    [Fact]
    public void HandleKeyDown_Delete_CallsDelete()
    {
        // Arrange
        var cursorPosition = 5;
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(cursorPosition);
        _textBufferServiceMock.Setup(t => t.Length).Returns(10);

        // Act
        _inputService.HandleKeyDown(Key.Delete);

        // Assert
        _textBufferServiceMock.Verify(t => t.Delete(cursorPosition, 1), Times.Once);
    }

    [Fact]
    public void HandleKeyDown_Left_CallsSetCursorPosition()
    {
        // Arrange
        var cursorPosition = 5;
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(cursorPosition);
        _textBufferServiceMock.Setup(t => t.Length).Returns(10);

        // Act
        _inputService.HandleKeyDown(Key.Left);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(cursorPosition - 1), Times.Once);
    }

    [Fact]
    public void HandleKeyDown_Right_CallsSetCursorPosition()
    {
        // Arrange
        var cursorPosition = 5;
        var textLength = 10;
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(cursorPosition);
        _textBufferServiceMock.Setup(t => t.Length).Returns(textLength);
        _textBufferServiceMock.Setup(t => t.GetText()).Returns(new string('a', textLength));

        // Act
        _inputService.HandleKeyDown(Key.Right);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(cursorPosition + 1), Times.Once);
    }

    [Fact]
    public void HandleKeyDown_Home_SetsCursorPositionToStart()
    {
        // Act
        _inputService.HandleKeyDown(Key.Home);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(0), Times.Once);
    }

    [Fact]
    public void HandleKeyDown_End_SetsCursorPositionToEnd()
    {
        // Arrange
        _textBufferServiceMock.Setup(t => t.Length).Returns(10);

        // Act
        _inputService.HandleKeyDown(Key.End);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(10), Times.Once);
    }

    [Fact]
    public void HandleKeyDown_Character_CallsInsertText()
    {
        // Arrange
        var cursorPosition = 5;
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(cursorPosition);
        _textBufferServiceMock.Setup(t => t.Length).Returns(10);

        // Act
        _inputService.HandleKeyDown(Key.A);

        // Output for debugging
        _output.WriteLine($"Test: Expected Insert Position: {cursorPosition}");
        _output.WriteLine("Test: Expected Insert Text: A");

        // Assert
        _textBufferServiceMock.Verify(t => t.Insert(cursorPosition, "A"), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(cursorPosition + 1), Times.Once);
    }

    [Fact]
    public void HandleKeyDown_UnknownKey_DoesNothing()
    {
        // Act
        _inputService.HandleKeyDown(Key.Unknown);

        // Assert
        _textBufferServiceMock.Verify(t => t.Insert(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        _textBufferServiceMock.Verify(t => t.Delete(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandleKeyDown_Enter_InsertsNewLine()
    {
        // Arrange
        var cursorPosition = 5;
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(cursorPosition);
        _textBufferServiceMock.Setup(t => t.Length).Returns(10);

        // Act
        _inputService.HandleKeyDown(Key.Enter);

        // Assert
        _textBufferServiceMock.Verify(t => t.Insert(cursorPosition, "\n"), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(cursorPosition + 1), Times.Once);
    }

    [Fact]
    public void HandleKeyDown_Backspace_AtBeginning_DoesNothing()
    {
        // Arrange
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(0);

        // Act
        _inputService.HandleKeyDown(Key.Backspace);

        // Assert
        _textBufferServiceMock.Verify(t => t.Delete(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandleKeyDown_Delete_AtEnd_DoesNothing()
    {
        // Arrange
        var textLength = 10;
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(textLength);
        _textBufferServiceMock.Setup(t => t.Length).Returns(textLength);

        // Act
        _inputService.HandleKeyDown(Key.Delete);

        // Assert
        _textBufferServiceMock.Verify(t => t.Delete(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandleKeyDown_Left_AtBeginning_DoesNotMove()
    {
        // Arrange
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(0);

        // Act
        _inputService.HandleKeyDown(Key.Left);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandleKeyDown_Right_AtEnd_DoesNotMove()
    {
        // Arrange
        var textLength = 10;
        _cursorServiceMock.Setup(c => c.GetCursorPosition()).Returns(textLength);
        _textBufferServiceMock.Setup(t => t.Length).Returns(textLength);
        _textBufferServiceMock.Setup(t => t.GetText()).Returns(new string('a', textLength));

        // Act
        _inputService.HandleKeyDown(Key.Right);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandlePointerPressed_SingleClick_CallsHandleSingleClick()
    {
        // Arrange
        var e = new PointerPressedEventArgs { Index = 5 };

        // Act
        _inputService.HandlePointerPressed(e);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(5), Times.Once);
        _selectionServiceMock.Verify(s => s.StartSelection(5), Times.Once);
    }

    [Fact]
    public void HandlePointerPressed_DoubleClick_CallsHandleDoubleClick()
    {
        // Arrange
        var e = new PointerPressedEventArgs { Index = 5 };
        _textBufferServiceMock.Setup(t => t.GetText()).Returns("test text");
        _textAnalysisServiceMock.Setup(t => t.GetWordBoundariesAt(It.IsAny<string>(), It.IsAny<int>()))
            .Returns((3, 7));

        // Act
        _inputService.HandlePointerPressed(e);
        _inputService.HandlePointerPressed(e);

        // Assert
        _selectionServiceMock.Verify(s => s.SetSelection(3, 4), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(7), Times.Once);
    }

    [Fact]
    public void HandlePointerPressed_TripleClick_CallsHandleTripleClick()
    {
        // Arrange
        var e = new PointerPressedEventArgs { Index = 5 };
        _textBufferServiceMock.Setup(t => t.GetText()).Returns("test\ntext\n");
        _textAnalysisServiceMock.Setup(t => t.GetLineBoundariesAt(It.IsAny<string>(), It.IsAny<int>()))
            .Returns((5, 10));

        // Act
        _inputService.HandlePointerPressed(e);
        _inputService.HandlePointerPressed(e);
        _inputService.HandlePointerPressed(e);

        // Assert
        _selectionServiceMock.Verify(s => s.SetSelection(5, 5), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(10), Times.Once);
    }

    [Fact]
    public void HandlePointerMoved_UpdatesCursorAndSelection()
    {
        // Arrange
        var e = new PointerEventArgs { X = 10, Y = 20, Index = 5 };

        // Act
        _inputService.HandlePointerMoved(e);

        // Assert
        _cursorServiceMock.Verify(c => c.MoveCursor(10, 20), Times.Once);
        _selectionServiceMock.Verify(s => s.UpdateSelection(5), Times.Once);
    }

    [Fact]
    public void HandlePointerReleased_UpdatesSelection()
    {
        // Arrange
        var e = new PointerReleasedEventArgs { Index = 5 };

        // Act
        _inputService.HandlePointerReleased(e);

        // Assert
        _selectionServiceMock.Verify(s => s.UpdateSelection(5), Times.Once);
    }
}