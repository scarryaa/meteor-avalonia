using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.Events;
using meteor.Core.Services;
using Moq;
using Xunit.Abstractions;

namespace meteor.UnitTests.Application.Services;

public class InputServiceTests
{
    private readonly Mock<IClipboardService> _clipboardServiceMock;
    private readonly Mock<ICursorService> _cursorServiceMock;
    private readonly InputService _inputService;
    private readonly Mock<ISelectionService> _selectionServiceMock;
    private readonly Mock<ITabService> _tabServiceMock;
    private readonly Mock<ITextAnalysisService> _textAnalysisServiceMock;
    private readonly Mock<ITextBufferService> _textBufferServiceMock;
    private readonly Mock<ITextMeasurer> _textMeasurerMock;

    public InputServiceTests(ITestOutputHelper output)
    {
        _tabServiceMock = new Mock<ITabService>();
        _textBufferServiceMock = new Mock<ITextBufferService>();
        _cursorServiceMock = new Mock<ICursorService>();
        _textAnalysisServiceMock = new Mock<ITextAnalysisService>();
        _selectionServiceMock = new Mock<ISelectionService>();
        _clipboardServiceMock = new Mock<IClipboardService>();
        _textMeasurerMock = new Mock<ITextMeasurer>();
        _inputService = new InputService(
            _tabServiceMock.Object,
            _cursorServiceMock.Object,
            _textAnalysisServiceMock.Object,
            _selectionServiceMock.Object,
            _clipboardServiceMock.Object,
            _textMeasurerMock.Object);

        _tabServiceMock.Setup(ts => ts.GetActiveTextBufferService()).Returns(_textBufferServiceMock.Object);
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

        // Act
        _inputService.HandleKeyDown(Key.Right);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandlePointerPressed_SingleClick_CallsHandleSingleClick()
    {
        // Arrange
        var e = new PointerPressedEventArgs { X = 10, Y = 20 };
        _textBufferServiceMock.Setup(t => t.Length).Returns(11);
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 10, 20, 0, 0)).Returns(5);

        // Act
        _inputService.HandlePointerPressed(e);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(5), Times.Once);
        _selectionServiceMock.Verify(s => s.StartSelection(5), Times.Once);
    }

    [Fact]
    public void HandlePointerPressed_ClearsExistingSelectionAndStartsNewOne()
    {
        // Arrange
        var pressEvent = new PointerPressedEventArgs { X = 10, Y = 20 };
        _textBufferServiceMock.Setup(t => t.Length).Returns(11);
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 10, 20, 0, 0)).Returns(5);

        // Act
        _inputService.HandlePointerPressed(pressEvent);

        // Assert
        _selectionServiceMock.Verify(s => s.ClearSelection(), Times.Once);
        _selectionServiceMock.Verify(s => s.StartSelection(5), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(5), Times.Once);
    }

    [Fact]
    public void HandlePointerPressed_DoubleClick_CallsHandleDoubleClick()
    {
        // Arrange
        var e = new PointerPressedEventArgs { X = 10, Y = 20 };
        _textBufferServiceMock.Setup(t => t.Length).Returns(11);
        _textAnalysisServiceMock.Setup(t => t.GetWordBoundaries(_textBufferServiceMock.Object, It.IsAny<int>()))
            .Returns((0, 11));

        // Act
        _inputService.HandlePointerPressed(e);
        _inputService.HandlePointerPressed(e);

        // Assert
        _selectionServiceMock.Verify(s => s.SetSelection(0, 11), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(11), Times.Once);
    }

    [Fact]
    public void HandlePointerPressed_TripleClick_CallsHandleTripleClick()
    {
        // Arrange
        var e = new PointerPressedEventArgs { X = 10, Y = 20 };
        _textBufferServiceMock.Setup(t => t.Length).Returns(11);
        _textAnalysisServiceMock.Setup(t => t.GetWordBoundaries(_textBufferServiceMock.Object, It.IsAny<int>()))
            .Returns((0, 11));

        // Act
        _inputService.HandlePointerPressed(e);
        _inputService.HandlePointerPressed(e);
        _inputService.HandlePointerPressed(e);

        // Assert
        _selectionServiceMock.Verify(s => s.SetSelection(0, 11), Times.Exactly(1));
        _cursorServiceMock.Verify(c => c.SetCursorPosition(11), Times.Exactly(1));
    }

    [Fact]
    public void HandlePointerMoved_WithoutLeftButtonPressed_DoesNotUpdateCursorAndSelection()
    {
        // Arrange
        var e = new PointerEventArgs { X = 10, Y = 20, IsLeftButtonPressed = false };
        _textBufferServiceMock.Setup(t => t.Length).Returns(11);
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 10, 20, 0, 0)).Returns(5);

        // Act
        _inputService.HandlePointerMoved(e);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(It.IsAny<int>()), Times.Never);
        _selectionServiceMock.Verify(s => s.UpdateSelection(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandlePointerPressed_SingleClick_StartsSelectionAndSetsCursor()
    {
        // Arrange
        var e = new PointerPressedEventArgs { X = 10, Y = 20 };
        _textBufferServiceMock.Setup(t => t.Length).Returns(11);
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 10, 20, 0, 0)).Returns(5);

        // Act
        _inputService.HandlePointerPressed(e);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(5), Times.Once);
        _selectionServiceMock.Verify(s => s.StartSelection(5), Times.Once);

        // Additional assertions to differentiate this test
        _selectionServiceMock.Verify(s => s.ClearSelection(), Times.Once);
        _selectionServiceMock.Verify(s => s.SetSelection(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandlePointerPressed_DoubleClick_SelectsWord()
    {
        // Arrange
        var e = new PointerPressedEventArgs { X = 10, Y = 20 };
        _textBufferServiceMock.Setup(t => t.Length).Returns(11);
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 10, 20, 0, 0))
            .Returns(5); // Assume click is at index 5
        _textAnalysisServiceMock.Setup(t => t.GetWordBoundaries(It.IsAny<ITextBufferService>(), 5))
            .Returns((3, 8)); // Assume word boundaries are 3 and 8

        // Act
        _inputService.HandlePointerPressed(e);
        _inputService.HandlePointerPressed(e);

        // Assert
        _selectionServiceMock.Verify(s => s.SetSelection(3, 5), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(8), Times.Once);
    }

    [Fact]
    public void HandlePointerPressed_TripleClick_SelectsLine()
    {
        // Arrange
        var e = new PointerPressedEventArgs { X = 10, Y = 20 };
        _textBufferServiceMock.Setup(t => t.Length).Returns(30);
        _textAnalysisServiceMock.Setup(t => t.GetLineBoundaries(_textBufferServiceMock.Object, It.IsAny<int>()))
            .Returns((5, 25));

        // Act
        _inputService.HandlePointerPressed(e);
        _inputService.HandlePointerPressed(e);
        _inputService.HandlePointerPressed(e);

        // Assert
        _selectionServiceMock.Verify(s => s.SetSelection(5, 20), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(25), Times.Once);
        _textAnalysisServiceMock.Verify(t => t.GetLineBoundaries(_textBufferServiceMock.Object, It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public void HandlePointerMoved_WithLeftButtonPressed_UpdatesSelectionAndCursor()
    {
        // Arrange
        var pressEvent = new PointerPressedEventArgs { X = 0, Y = 0 };
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 0, 0, 0, 0)).Returns(0);

        var moveEvent = new PointerEventArgs { X = 10, Y = 20, IsLeftButtonPressed = true };
        _textBufferServiceMock.Setup(t => t.Length).Returns(11);
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 10, 20, 0, 0)).Returns(5);

        // Act
        _inputService.HandlePointerPressed(pressEvent);
        _inputService.HandlePointerMoved(moveEvent);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(5), Times.Once);
        _selectionServiceMock.Verify(s => s.SetSelection(0, 5), Times.Once);
    }

    [Fact]
    public void HandlePointerMoved_WithoutLeftButtonPressed_DoesNotUpdateSelectionAndCursor()
    {
        // Arrange
        var moveEvent = new PointerEventArgs { X = 10, Y = 20, IsLeftButtonPressed = false };
        _textBufferServiceMock.Setup(t => t.Length).Returns(11);
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 10, 20, 0, 0)).Returns(5);

        // Act
        _inputService.HandlePointerMoved(moveEvent);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(It.IsAny<int>()), Times.Never);
        _selectionServiceMock.Verify(s => s.SetSelection(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandlePointerReleased_DoesNothing()
    {
        // Arrange
        var e = new PointerReleasedEventArgs { X = 10, Y = 20 };

        // Act
        _inputService.HandlePointerReleased(e);

        // Assert
        _cursorServiceMock.Verify(c => c.SetCursorPosition(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandlePointerEvents_ComplexSelectionScenario()
    {
        // Arrange
        _textBufferServiceMock.Setup(t => t.Length).Returns(19);

        // Simulate press at the beginning
        var pressEvent = new PointerPressedEventArgs { X = 0, Y = 0 };
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 0, 0, 0, 0)).Returns(0);

        // Simulate move to the middle
        var moveEvent1 = new PointerEventArgs { X = 10, Y = 0, IsLeftButtonPressed = true };
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 10, 0, 0, 0)).Returns(10);

        // Simulate move to the end
        var moveEvent2 = new PointerEventArgs { X = 20, Y = 0, IsLeftButtonPressed = true };
        _textMeasurerMock.Setup(m => m.GetIndexAtPosition(It.IsAny<ITextBufferService>(), 20, 0, 0, 0)).Returns(19);

        // Act
        _inputService.HandlePointerPressed(pressEvent);
        _inputService.HandlePointerMoved(moveEvent1);
        _inputService.HandlePointerMoved(moveEvent2);

        // Assert
        _selectionServiceMock.Verify(s => s.ClearSelection(), Times.Once);
        _selectionServiceMock.Verify(s => s.StartSelection(0), Times.Once);
        _selectionServiceMock.Verify(s => s.SetSelection(0, 10), Times.Once);
        _selectionServiceMock.Verify(s => s.SetSelection(0, 19), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(0), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(10), Times.Once);
        _cursorServiceMock.Verify(c => c.SetCursorPosition(19), Times.Once);
    }
}