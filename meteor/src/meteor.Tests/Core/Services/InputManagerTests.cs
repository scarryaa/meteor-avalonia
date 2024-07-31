using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.EventArgs;
using meteor.Core.Services;
using Moq;

namespace meteor.Tests.Core.Services;

public class InputManagerTests
{
    private readonly InputManager _inputManager;
    private readonly Mock<IClipboardManager> _mockClipboardManager;
    private readonly Mock<ICursorManager> _mockCursorManager;
    private readonly Mock<IScrollManager> _mockScrollManager;
    private readonly Mock<ISelectionManager> _mockSelectionManager;
    private readonly Mock<ITextAnalysisService> _mockTextAnalysisService;
    private readonly Mock<ITextBufferService> _mockTextBufferService;

    public InputManagerTests()
    {
        _mockTextBufferService = new Mock<ITextBufferService>();
        _mockCursorManager = new Mock<ICursorManager>();
        _mockClipboardManager = new Mock<IClipboardManager>();
        _mockSelectionManager = new Mock<ISelectionManager>();
        _mockTextAnalysisService = new Mock<ITextAnalysisService>();
        _mockScrollManager = new Mock<IScrollManager>();

        _inputManager = new InputManager(
            _mockTextBufferService.Object,
            _mockCursorManager.Object,
            _mockClipboardManager.Object,
            _mockSelectionManager.Object,
            _mockTextAnalysisService.Object,
            _mockScrollManager.Object);
    }

    [Fact]
    public async Task HandleKeyDown_EnterKey_InsertsNewlineAndMovesCursor()
    {
        // Arrange
        var e = new KeyEventArgs { Key = Key.Enter };
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);

        // Act
        await _inputManager.HandleKeyDown(e);

        // Assert
        _mockTextBufferService.Verify(tbs => tbs.InsertText(5, "\n"), Times.Once);
        _mockCursorManager.Verify(cm => cm.MoveCursor(1), Times.Once);
        _mockTextAnalysisService.Verify(tas => tas.ResetDesiredColumn(), Times.Once);
        Assert.True(e.Handled);
    }

    [Fact]
    public async Task HandleKeyDown_LeftArrow_MovesCursorLeft()
    {
        // Arrange
        var e = new KeyEventArgs { Key = Key.Left };
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);
        _mockSelectionManager.Setup(sm => sm.HasSelection).Returns(false);

        // Act
        await _inputManager.HandleKeyDown(e);

        // Assert
        _mockCursorManager.Verify(cm => cm.SetPosition(4), Times.Once);
        Assert.True(e.Handled);
    }

    [Fact]
    public async Task HandleKeyDown_RightArrow_MovesCursorRight()
    {
        // Arrange
        var e = new KeyEventArgs { Key = Key.Right };
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);
        _mockSelectionManager.Setup(sm => sm.HasSelection).Returns(false);
        _mockTextBufferService.Setup(tbs => tbs.GetLength()).Returns(10);

        // Act
        await _inputManager.HandleKeyDown(e);

        // Assert
        _mockCursorManager.Verify(cm => cm.SetPosition(6), Times.Once);
        Assert.True(e.Handled);
    }

    [Fact]
    public async Task HandleKeyDown_Backspace_DeletesCharacterBeforeCursor()
    {
        // Arrange
        var e = new KeyEventArgs { Key = Key.Back };
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);
        _mockSelectionManager.Setup(sm => sm.HasSelection).Returns(false);

        // Act
        await _inputManager.HandleKeyDown(e);

        // Assert
        _mockTextBufferService.Verify(tbs => tbs.DeleteText(4, 1), Times.Once);
        _mockCursorManager.Verify(cm => cm.MoveCursor(-1), Times.Once);
        _mockTextAnalysisService.Verify(tas => tas.ResetDesiredColumn(), Times.Once);
        Assert.True(e.Handled);
    }

    [Fact]
    public async Task HandleKeyDown_Delete_DeletesCharacterAtCursor()
    {
        // Arrange
        var e = new KeyEventArgs { Key = Key.Delete };
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);
        _mockSelectionManager.Setup(sm => sm.HasSelection).Returns(false);
        _mockTextBufferService.Setup(tbs => tbs.GetLength()).Returns(10);

        // Act
        await _inputManager.HandleKeyDown(e);

        // Assert
        _mockTextBufferService.Verify(tbs => tbs.DeleteText(5, 1), Times.Once);
        Assert.True(e.Handled);
    }

    [Fact]
    public async Task HandleKeyDown_Home_MovesCursorToStartOfLine()
    {
        // Arrange
        var e = new KeyEventArgs { Key = Key.Home };
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);
        _mockTextAnalysisService.Setup(tas => tas.FindStartOfCurrentLine(It.IsAny<string>(), 5)).Returns(0);

        // Act
        await _inputManager.HandleKeyDown(e);

        // Assert
        _mockCursorManager.Verify(cm => cm.SetPosition(0), Times.Once);
        Assert.True(e.Handled);
    }

    [Fact]
    public async Task HandleKeyDown_End_MovesCursorToEndOfLine()
    {
        // Arrange
        var e = new KeyEventArgs { Key = Key.End };
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);
        _mockTextAnalysisService.Setup(tas => tas.FindEndOfCurrentLine(It.IsAny<string>(), 5)).Returns(10);

        // Act
        await _inputManager.HandleKeyDown(e);

        // Assert
        _mockCursorManager.Verify(cm => cm.SetPosition(10), Times.Once);
        Assert.True(e.Handled);
    }

    [Fact]
    public async Task HandleKeyDown_CtrlA_SelectsAllText()
    {
        // Arrange
        var e = new KeyEventArgs { Key = Key.A, Modifiers = KeyModifiers.Control };
        _mockTextBufferService.Setup(tbs => tbs.GetLength()).Returns(100);

        // Act
        await _inputManager.HandleKeyDown(e);

        // Assert
        _mockSelectionManager.Verify(sm => sm.SetSelection(0, 100), Times.Once);
        _mockCursorManager.Verify(cm => cm.SetPosition(100), Times.Once);
        _mockTextAnalysisService.Verify(tas => tas.ResetDesiredColumn(), Times.Once);
        Assert.True(e.Handled);
    }

    [Fact]
    public async Task HandleKeyDown_CtrlC_CopiesSelectedText()
    {
        // Arrange
        var e = new KeyEventArgs { Key = Key.C, Modifiers = KeyModifiers.Control };
        _mockSelectionManager.Setup(sm => sm.HasSelection).Returns(true);
        _mockSelectionManager.Setup(sm => sm.GetSelectedText(It.IsAny<ITextBufferService>())).Returns("Selected Text");

        // Act
        await _inputManager.HandleKeyDown(e);

        // Assert
        _mockClipboardManager.Verify(cm => cm.CopyAsync("Selected Text"), Times.Once);
        Assert.True(e.Handled);
    }

    [Fact]
    public async Task HandleKeyDown_CtrlV_PastesClipboardContent()
    {
        // Arrange
        var e = new KeyEventArgs { Key = Key.V, Modifiers = KeyModifiers.Control };
        _mockClipboardManager.Setup(cm => cm.PasteAsync()).ReturnsAsync("Pasted Text");
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);

        // Act
        await _inputManager.HandleKeyDown(e);

        // Assert
        _mockTextBufferService.Verify(tbs => tbs.InsertText(5, "Pasted Text"), Times.Once);
        _mockCursorManager.Verify(cm => cm.MoveCursor(11), Times.Once);
        _mockTextAnalysisService.Verify(tas => tas.ResetDesiredColumn(), Times.Once);
        Assert.True(e.Handled);
    }

    [Fact]
    public void HandleTextInput_InsertsTextAndMovesCursor()
    {
        // Arrange
        var e = new TextInputEventArgs { Text = "a" };
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);

        // Act
        _inputManager.HandleTextInput(e);

        // Assert
        _mockTextBufferService.Verify(tbs => tbs.InsertText(5, "a"), Times.Once);
        _mockCursorManager.Verify(cm => cm.MoveCursor(1), Times.Once);
        _mockTextAnalysisService.Verify(tas => tas.ResetDesiredColumn(), Times.Once);
        Assert.True(e.Handled);
    }
}