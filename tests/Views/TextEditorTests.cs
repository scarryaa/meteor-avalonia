using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using meteor.Interfaces;
using meteor.ViewModels;
using meteor.Views;
using Moq;
using tests.Mocks;

namespace tests.Views;

public class TextEditorTests
{
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly Mock<ICursorPositionService> _mockCursorPositionService;
    private readonly ScrollableTextEditorViewModel _scrollableViewModel;
    private readonly TextEditor _textEditor;
    private readonly Mock<IClipboardService> _mockClipboardService;
    private readonly Mock<IClipboard> _mockClipboard;
    private readonly MockTopLevel _mockTopLevel;

    public TextEditorTests()
    {
        _mockClipboardService = new Mock<IClipboardService>();
        _mockClipboard = new Mock<IClipboard>();

        // Mock the necessary dependencies for ScrollableTextEditorViewModel
        var mockCursorPositionService = new Mock<ICursorPositionService>();
        var fontPropertiesViewModel = new FontPropertiesViewModel();
        var lineCountViewModel = new LineCountViewModel();
        _mockTextBuffer = new Mock<ITextBuffer>();

        _mockTextBuffer.Setup(tb => tb.Rope).Returns(new Mock<IRope>().Object);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(18); // Total length of "Line 1\nLine 2\nLine 3"
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(3);
        _mockTextBuffer.Setup(tb => tb.GetLineText(It.IsAny<long>())).Returns<long>(lineIndex => lineIndex switch
        {
            0 => "Line 1",
            1 => "Line 2",
            2 => "Line 3",
            _ => string.Empty
        });
        _mockTextBuffer.Setup(tb => tb.LineStarts).Returns(new List<long> { 0, 7, 14 }); // Include newline characters

        _mockTopLevel = new MockTopLevel();
        _mockTopLevel.MockClipboard = _mockClipboard.Object;

        _scrollableViewModel = new ScrollableTextEditorViewModel(
            mockCursorPositionService.Object,
            fontPropertiesViewModel,
            lineCountViewModel,
            _mockTextBuffer.Object,
            _mockClipboardService.Object);

        _textEditor = new TextEditor
        {
            DataContext = _scrollableViewModel
        };

        // Use reflection to set the private field
        var field = typeof(TextEditor).GetField("_scrollableViewModel", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(_textEditor, _scrollableViewModel);
    }

    [AvaloniaFact]
    public void Constructor_InitializesProperties()
    {
        Assert.NotNull(_textEditor.FontFamily);
        Assert.Equal(13, _textEditor.FontSize);
        Assert.Equal(20, _textEditor.LineHeight);
    }

    [AvaloniaFact]
    public void UpdateHeight_SetsHeight()
    {
        const double newHeight = 100;
        _textEditor.UpdateHeight(newHeight);
        Assert.Equal(newHeight, _textEditor.Height);
    }

    [AvaloniaFact]
    public async Task HandleTextInput_InsertsTextAndUpdatesCursor()
    {
        // Arrange
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 3; // Middle of "Line 1"
        var textInputArgs = new TextInputEventArgs
        {
            Text = "Hello"
        };

        // Act
        await RaiseTextInputEvent(_textEditor, textInputArgs);

        // Assert
        _mockTextBuffer.Verify(tb => tb.InsertText(3, "Hello"), Times.Once);
        Assert.Equal(8, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_LeftArrow_MovesCursorLeft()
    {
        // Arrange
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 5;
        var keyEventArgs = new KeyEventArgs
        {
            Key = Key.Left
        };

        // Act
        await RaiseKeyEvent(_textEditor, keyEventArgs);

        // Assert
        Assert.Equal(4, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_RightArrow_MovesCursorRight()
    {
        // Arrange
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 5;

        var keyEventArgs = new KeyEventArgs
        {
            Key = Key.Right
        };

        // Act
        await RaiseKeyEvent(_textEditor, keyEventArgs);

        // Assert
        Assert.Equal(6, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_Backspace_DeletesCharacter()
    {
        // Arrange
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 5;

        var keyEventArgs = new KeyEventArgs
        {
            Key = Key.Back
        };

        // Act
        await RaiseKeyEvent(_textEditor, keyEventArgs);

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(4, 1), Times.Once);
        Assert.Equal(4, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_Delete_DeletesCharacter()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(20); // Correcting the total length
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 5;

        var keyEventArgs = new KeyEventArgs
        {
            Key = Key.Delete
        };

        // Act
        await RaiseKeyEvent(_textEditor, keyEventArgs);

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(5, 1), Times.Once);
        Assert.Equal(5, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_Home_MovesCursorToLineStart()
    {
        // Arrange
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 15; // Middle of "Line 2"
        _mockTextBuffer.Setup(tb => tb.Rope.GetLineIndexFromPosition(15)).Returns(1); // Line index for "Line 2"
        _mockTextBuffer.Setup(tb => tb.GetLineStartPosition(1)).Returns(7); // Start of "Line 2"

        var keyEventArgs = new KeyEventArgs { Key = Key.Home };

        // Act
        await RaiseKeyEvent(_textEditor, keyEventArgs);

        // Assert
        Assert.Equal(7, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }
    
    [AvaloniaFact]
    public async Task HandleKeyDown_End_MovesCursorToLineEnd()
    {
        // Arrange
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 8; // Middle of "Line 2"
        _mockTextBuffer.Setup(tb => tb.Rope.GetLineIndexFromPosition(8)).Returns(1); // Line index for "Line 2"
        _mockTextBuffer.Setup(tb => tb.GetLineStartPosition(1)).Returns(7); // Start of "Line 2"
        _mockTextBuffer.Setup(tb => tb.GetLineLength(1)).Returns(6); // Length of "Line 2"

        var keyEventArgs = new KeyEventArgs { Key = Key.End };

        // Act
        await RaiseKeyEvent(_textEditor, keyEventArgs);

        // Assert
        Assert.Equal(13, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task SelectAll_SelectsEntireText()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(18); // Total length of "Line 1\nLine 2\nLine 3"

        // Act
        _textEditor.SelectAll();

        // Assert
        Assert.Equal(0, _scrollableViewModel.TextEditorViewModel.SelectionStart);
        Assert.Equal(18, _scrollableViewModel.TextEditorViewModel.SelectionEnd);
        Assert.Equal(18, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task CopyText_CopiesSelectedTextToClipboard()
    {
        // Arrange
        const string expectedText = "Selected Text";
        _scrollableViewModel.TextEditorViewModel.SelectionStart = 0;
        _scrollableViewModel.TextEditorViewModel.SelectionEnd = 13;
        _mockTextBuffer.Setup(tb => tb.Rope.GetText(0, 13)).Returns(expectedText);

        // Act
        await _textEditor.CopyText();

        // Assert
        _mockClipboardService.Verify(c => c.SetTextAsync(expectedText), Times.Once);
    }

    [AvaloniaFact]
    public async Task PasteText_InsertsClipboardTextAtCursor()
    {
        // Arrange
        const string expectedText = "Pasted Text";
        const long cursorPosition = 5;

        _scrollableViewModel.TextEditorViewModel.CursorPosition = cursorPosition;
        _mockClipboardService.Setup(cs => cs.GetTextAsync()).ReturnsAsync(expectedText);

        // Act
        await _scrollableViewModel.TextEditorViewModel.PasteText();

        // Assert
        _mockTextBuffer.Verify(tb => tb.InsertText(cursorPosition, expectedText), Times.Once);
    }

    private async Task RaiseTextInputEvent(TextEditor textEditor, TextInputEventArgs args)
    {
        var method = typeof(TextEditor).GetMethod("OnTextInput", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            var task = (Task)method.Invoke(textEditor, new object[] { args });
            if (task != null)
                await task;
        }
        else
        {
            throw new InvalidOperationException("Method OnTextInput not found");
        }
    }

    private async Task RaiseKeyEvent(TextEditor textEditor, KeyEventArgs args)
    {
        var method = typeof(TextEditor).GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            var task = (Task)method.Invoke(textEditor, new object[] { args });
            if (task != null)
                await task;
        }
        else
        {
            throw new InvalidOperationException("Method OnKeyDown not found");
        }
    }
}
