using meteor.Core.Enums;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Interfaces.Events;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Models.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace meteor.Services.Tests;

public class InputManagerTests
{
    private readonly Mock<ICursorManager> _mockCursorManager;
    private readonly Mock<ISelectionHandler> _mockSelectionHandler;
    private readonly Mock<ITextEditorCommands> _mockEditorCommands;
    private readonly Mock<ILogger<InputManager>> _mockLogger;
    private readonly Mock<IEventAggregator> _mockEventAggregator;
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly InputManager _inputManager;

    public InputManagerTests()
    {
        _mockCursorManager = new Mock<ICursorManager>();
        _mockSelectionHandler = new Mock<ISelectionHandler>();
        _mockEditorCommands = new Mock<ITextEditorCommands>();
        _mockLogger = new Mock<ILogger<InputManager>>();
        _mockEventAggregator = new Mock<IEventAggregator>();
        _mockTextBuffer = new Mock<ITextBuffer>();

        _inputManager = new InputManager(
            _mockCursorManager.Object,
            _mockSelectionHandler.Object,
            _mockEditorCommands.Object,
            _mockLogger.Object,
            _mockEventAggregator.Object,
            _mockTextBuffer.Object
        );
    }

    [Fact]
    public void OnPointerPressed_SingleClick_ShouldSetCursorAndStartSelection()
    {
        // Arrange
        var mockEventArgs = new Mock<IPointerPressedEventArgs>();
        var point = new Mock<IPoint>();
        mockEventArgs.Setup(e => e.GetPosition()).Returns(point.Object);
        _mockEditorCommands.Setup(ec => ec.GetPositionFromPoint(It.IsAny<IPoint>())).Returns(5);

        // Act
        _inputManager.OnPointerPressed(mockEventArgs.Object);

        // Assert
        _mockCursorManager.Verify(cm => cm.SetPosition(5), Times.Once);
        _mockSelectionHandler.Verify(sh => sh.StartSelection(5), Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<IsSelectingChangedEventArgs>()), Times.Once);
        mockEventArgs.VerifySet(e => e.Handled = true, Times.Once);
    }

    [Fact]
    public void OnPointerPressed_DoubleClick_ShouldSelectWord()
    {
        // Arrange
        var mockEventArgs = new Mock<IPointerPressedEventArgs>();
        var point = new Mock<IPoint>();
        point.Setup(p => p.X).Returns(0);
        point.Setup(p => p.Y).Returns(0);
        mockEventArgs.Setup(e => e.GetPosition()).Returns(point.Object);
        _mockEditorCommands.Setup(ec => ec.GetPositionFromPoint(It.IsAny<IPoint>())).Returns(5);

        // Simulate first click
        _inputManager.OnPointerPressed(mockEventArgs.Object);

        // Act - Simulate second click within threshold
        _inputManager.OnPointerPressed(mockEventArgs.Object);

        // Assert
        _mockSelectionHandler.Verify(sh => sh.SelectWord(5), Times.Once);
        Assert.True(_inputManager.IsDoubleClickDrag);
        mockEventArgs.VerifySet(e => e.Handled = true, Times.Exactly(2));
    }

    [Fact]
    public void OnPointerMoved_WhileSelecting_ShouldUpdateSelection()
    {
        // Arrange
        var mockEventArgs = new Mock<IPointerEventArgs>();
        var point = new Mock<IPoint>();
        mockEventArgs.Setup(e => e.GetPosition()).Returns(point.Object);
        _mockEditorCommands.Setup(ec => ec.GetPositionFromPoint(It.IsAny<IPoint>())).Returns(10);
        _mockSelectionHandler.Setup(sh => sh.IsSelecting).Returns(true);

        // Act
        _inputManager.OnPointerMoved(mockEventArgs.Object);

        // Assert
        _mockSelectionHandler.Verify(sh => sh.UpdateSelectionDuringDrag(10, false, false), Times.Once);
        mockEventArgs.VerifySet(e => e.Handled = true, Times.Once);
    }

    [Fact]
    public void OnPointerReleased_ShouldEndSelectionAndResetDragFlags()
    {
        // Arrange
        var mockEventArgs = new Mock<IPointerReleasedEventArgs>();
        _inputManager.IsDoubleClickDrag = true;
        _inputManager.IsTripleClickDrag = true;

        // Act
        _inputManager.OnPointerReleased(mockEventArgs.Object);

        // Assert
        _mockSelectionHandler.Verify(sh => sh.EndSelection(), Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<IsSelectingChangedEventArgs>(e => e.IsSelecting == false)),
            Times.Once);
        Assert.False(_inputManager.IsDoubleClickDrag);
        Assert.False(_inputManager.IsTripleClickDrag);
        mockEventArgs.VerifySet(e => e.Handled = true, Times.Once);
    }

    [Fact]
    public async Task OnKeyDown_ArrowKeys_ShouldMoveCursor()
    {
        // Arrange
        var mockEventArgs = new Mock<IKeyEventArgs>();
        mockEventArgs.Setup(e => e.Key).Returns(Key.Right);
        mockEventArgs.Setup(e => e.IsShiftPressed).Returns(false);

        // Act
        await _inputManager.OnKeyDown(mockEventArgs.Object);

        // Assert
        _mockCursorManager.Verify(cm => cm.MoveCursorRight(false), Times.Once);
        mockEventArgs.VerifySet(e => e.Handled = true, Times.Once);
    }

    [Fact]
    public async Task OnKeyDown_Backspace_ShouldHandleBackspace()
    {
        // Arrange
        var mockEventArgs = new Mock<IKeyEventArgs>();
        mockEventArgs.Setup(e => e.Key).Returns(Key.Back);

        // Act
        await _inputManager.OnKeyDown(mockEventArgs.Object);

        // Assert
        _mockEditorCommands.Verify(ec => ec.HandleBackspace(), Times.Once);
        mockEventArgs.VerifySet(e => e.Handled = true, Times.Once);
    }

    [Fact]
    public async Task OnKeyDown_CtrlC_ShouldCopyText()
    {
        // Arrange
        var mockEventArgs = new Mock<IKeyEventArgs>();
        mockEventArgs.Setup(e => e.Key).Returns(Key.C);
        mockEventArgs.Setup(e => e.IsControlPressed).Returns(true);

        // Act
        await _inputManager.OnKeyDown(mockEventArgs.Object);

        // Assert
        _mockEditorCommands.Verify(ec => ec.CopyText(), Times.Once);
        mockEventArgs.VerifySet(e => e.Handled = true, Times.Once);
    }

    [Fact]
    public void OnTextInput_ShouldInsertTextAndPublishEvents()
    {
        // Arrange
        var mockEventArgs = new Mock<ITextInputEventArgs>();
        mockEventArgs.Setup(e => e.Text).Returns("a");
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);

        // Act
        _inputManager.OnTextInput(mockEventArgs.Object);

        // Assert
        _mockEditorCommands.Verify(ec => ec.InsertText(5, "a"), Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<TextChangedEventArgs>(e =>
            e.Position == 5 && e.InsertedText == "a" && e.DeletedLength == 0)), Times.Once);
        mockEventArgs.VerifySet(e => e.Handled = true, Times.Once);
    }
}