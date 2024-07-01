using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using meteor.Interfaces;
using meteor.ViewModels;
using meteor.Views;
using Moq;

namespace tests.Views;

public class TextEditorTests
{
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly Mock<ICursorPositionService> _mockCursorPositionService;
    private readonly ScrollableTextEditorViewModel _scrollableViewModel;
    private readonly TextEditor _textEditor;
    private readonly Mock<TopLevel> _mockTopLevel;
    private readonly Mock<IClipboardService> _mockClipboardService;
    private readonly Mock<IRope> _mockRope;

    public TextEditorTests()
    {
        _mockClipboardService = new Mock<IClipboardService>();

        var mockCursorPositionService = new Mock<ICursorPositionService>();
        var fontPropertiesViewModel = new FontPropertiesViewModel();
        var lineCountViewModel = new LineCountViewModel();
        _mockRope = new Mock<IRope>();
        _mockTextBuffer = new Mock<ITextBuffer>();

        _mockTextBuffer.Setup(tb => tb.Rope).Returns(_mockRope.Object);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(0);
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(1);
        _mockTextBuffer.Setup(tb => tb.GetLineText(It.IsAny<long>())).Returns(string.Empty);
        _mockTextBuffer.Setup(tb => tb.LineStarts).Returns([0]);

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
    public void OnTextChanged_InvalidatesLineAndVisual()
    {
        const long lineIndex = 0;
        _textEditor.OnTextChanged(lineIndex);

        // check if PropertyChanged was raised
        Assert.PropertyChanged(_textEditor, nameof(TextEditor.InvalidateVisual),
            () => _textEditor.OnTextChanged(lineIndex));
    }

    [AvaloniaFact]
    public async Task HandleTextInput_InsertsTextAndUpdatesCursor()
    {
        var textInputArgs = new TextInputEventArgs
        {
            Text = "Hello"
        };

        await RaiseTextInputEvent(_textEditor, textInputArgs);

        _mockTextBuffer.Verify(tb => tb.InsertText(0, "Hello"), Times.Once);
        Assert.Equal(5, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_LeftArrow_MovesCursorLeft()
    {
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 5;

        var keyEventArgs = new KeyEventArgs
        {
            Key = Key.Left
        };

        await RaiseKeyEvent(_textEditor, keyEventArgs);

        Assert.Equal(4, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_RightArrow_MovesCursorRight()
    {
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);

        var keyEventArgs = new KeyEventArgs
        {
            Key = Key.Right
        };

        await RaiseKeyEvent(_textEditor, keyEventArgs);

        Assert.Equal(1, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_Backspace_DeletesCharacter()
    {
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 5;

        var keyEventArgs = new KeyEventArgs
        {
            Key = Key.Back
        };

        await RaiseKeyEvent(_textEditor, keyEventArgs);

        _mockTextBuffer.Verify(tb => tb.DeleteText(4, 1), Times.Once);
        Assert.Equal(4, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_Delete_DeletesCharacter()
    {
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 5;

        var keyEventArgs = new KeyEventArgs
        {
            Key = Key.Delete
        };

        await RaiseKeyEvent(_textEditor, keyEventArgs);

        _mockTextBuffer.Verify(tb => tb.DeleteText(5, 1), Times.Once);
        Assert.Equal(5, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_Home_MovesCursorToLineStart()
    {
        _mockTextBuffer.Setup(tb => tb.LineStarts).Returns([0, 10, 20]);
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 15;

        var keyEventArgs = new KeyEventArgs
        {
            Key = Key.Home
        };

        await RaiseKeyEvent(_textEditor, keyEventArgs);

        Assert.Equal(10, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task HandleKeyDown_End_MovesCursorToLineEnd()
    {
        _mockTextBuffer.Setup(tb => tb.LineStarts).Returns([0, 10, 20]);
        _mockTextBuffer.Setup(tb => tb.GetLineLength(It.IsAny<long>())).Returns(10);
        _scrollableViewModel.TextEditorViewModel.CursorPosition = 15;

        var keyEventArgs = new KeyEventArgs
        {
            Key = Key.End
        };

        await RaiseKeyEvent(_textEditor, keyEventArgs);

        Assert.Equal(20, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public void SelectAll_SelectsEntireText()
    {
        _mockTextBuffer.Setup(tb => tb.Length).Returns(100);

        _textEditor.SelectAll();

        Assert.Equal(0, _scrollableViewModel.TextEditorViewModel.SelectionStart);
        Assert.Equal(100, _scrollableViewModel.TextEditorViewModel.SelectionEnd);
        Assert.Equal(100, _scrollableViewModel.TextEditorViewModel.CursorPosition);
    }

    [AvaloniaFact]
    public async Task CopyText_CopiesSelectedTextToClipboard()
    {
        // Arrange
        const string expectedText = "Selected Text";
        _scrollableViewModel.TextEditorViewModel.SelectionStart = 10;
        _scrollableViewModel.TextEditorViewModel.SelectionEnd = 23;
        var mockTextBuffer = Mock.Get(_scrollableViewModel.TextEditorViewModel.TextBuffer);
        mockTextBuffer.Setup(tb => tb.GetText(It.IsAny<int>(), It.IsAny<int>())).Returns(expectedText);

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
        await (Task)method.Invoke(textEditor, new object[] { args });
    }

    private async Task RaiseKeyEvent(TextEditor textEditor, KeyEventArgs args)
    {
        var method = typeof(TextEditor).GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method.Invoke(textEditor, new object[] { args });
    }
}