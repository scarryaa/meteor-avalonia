using System.Globalization;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using meteor.Interfaces;
using meteor.ViewModels;
using Moq;

namespace tests.ViewModels;

public class TextEditorViewModelTests
{
    private readonly Mock<ICursorPositionService> _mockCursorPositionService;
    private readonly Mock<FontPropertiesViewModel> _mockFontPropertiesViewModel;
    private readonly Mock<LineCountViewModel> _mockLineCountViewModel;
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly TextEditorViewModel _viewModel;
    private readonly Mock<IClipboardService> _mockClipboardService;

    public TextEditorViewModelTests()
    {
        _mockCursorPositionService = new Mock<ICursorPositionService>();
        _mockFontPropertiesViewModel = new Mock<FontPropertiesViewModel>();
        _mockFontPropertiesViewModel.Object.FontFamily = FontFamily.Default;
        _mockLineCountViewModel = new Mock<LineCountViewModel>();
        _mockTextBuffer = new Mock<ITextBuffer>();
        _mockClipboardService = new Mock<IClipboardService>();

        _viewModel = new TextEditorViewModel(
            _mockCursorPositionService.Object,
            _mockFontPropertiesViewModel.Object,
            _mockLineCountViewModel.Object,
            _mockTextBuffer.Object,
            _mockClipboardService.Object
        );
        
    }

    [AvaloniaFact]
    public void Constructor_InitializesProperties()
    {
        Assert.NotNull(_viewModel.FontPropertiesViewModel);
        Assert.Equal(_mockTextBuffer.Object, _viewModel.TextBuffer);
    }

    [AvaloniaFact]
    public void CursorPosition_UpdatesAndNotifiesService()
    {
        long newPosition = 10;
        _viewModel.CursorPosition = newPosition;

        Assert.Equal(newPosition, _viewModel.CursorPosition);
        _mockCursorPositionService.Verify(s => s.UpdateCursorPosition(newPosition, It.IsAny<List<long>>()), Times.Once);
    }

    [AvaloniaFact]
    public void InsertText_UpdatesTextBufferAndCursorPosition()
    {
        long position = 5;
        var text = "Hello";
        _viewModel.InsertText(position, text);

        _mockTextBuffer.Verify(tb => tb.InsertText(position, text), Times.Once);
        Assert.Equal(position + text.Length, _viewModel.CursorPosition);
    }

    [AvaloniaFact]
    public void DeleteText_CallsTextBufferDelete()
    {
        long start = 5;
        long length = 3;
        _viewModel.DeleteText(start, length);

        _mockTextBuffer.Verify(tb => tb.DeleteText(start, length), Times.Once);
    }

    [AvaloniaFact]
    public void ClearSelection_SetsBothSelectionPointsToCursorPosition()
    {
        _viewModel.CursorPosition = 10;
        _viewModel.SelectionStart = 5;
        _viewModel.SelectionEnd = 15;

        _viewModel.ClearSelection();

        Assert.Equal(_viewModel.CursorPosition, _viewModel.SelectionStart);
        Assert.Equal(_viewModel.CursorPosition, _viewModel.SelectionEnd);
    }

    [AvaloniaFact]
    public void UpdateLineStarts_CallsTextBufferUpdateLineCache()
    {
        // Reset the mock to clear the call from the constructor
        _mockTextBuffer.Invocations.Clear();

        _viewModel.UpdateLineStarts();

        _mockTextBuffer.Verify(tb => tb.UpdateLineCache(), Times.Once);
    }

    [AvaloniaFact]
    public void WindowHeight_UpdatesPropertyCorrectly()
    {
        var newHeight = 100.5;
        _viewModel.WindowHeight = newHeight;

        Assert.Equal(newHeight, _viewModel.WindowHeight);
    }

    [AvaloniaFact]
    public void WindowWidth_UpdatesPropertyCorrectly()
    {
        var newWidth = 200.5;
        _viewModel.WindowWidth = newWidth;

        Assert.Equal(newWidth, _viewModel.WindowWidth);
    }

    [AvaloniaFact]
    public void CharWidth_CalculatesCorrectly()
    {
        Assert.True(_viewModel.CharWidth > 0);
    }

    [AvaloniaFact]
    public void Focus_InvokesRequestFocusEvent()
    {
        var eventRaised = false;
        _viewModel.RequestFocus += (s, e) => eventRaised = true;

        _viewModel.Focus();

        Assert.True(eventRaised);
    }

    [AvaloniaFact]
    public void OnInvalidateRequired_InvokesInvalidateRequiredEvent()
    {
        var eventRaised = false;
        _viewModel.InvalidateRequired += (s, e) => eventRaised = true;

        _viewModel.OnInvalidateRequired();

        Assert.True(eventRaised);
    }

    [AvaloniaFact]
    public void UpdateLongestLineWidth_CalculatesMaxWidthCorrectly()
    {
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(3);
        _mockTextBuffer.Setup(tb => tb.GetLineText(0)).Returns("Short");
        _mockTextBuffer.Setup(tb => tb.GetLineText(1)).Returns("Much longer line of text");
        _mockTextBuffer.Setup(tb => tb.GetLineText(2)).Returns("Medium length");

        _viewModel.UpdateLongestLineWidth();

        var expectedWidth = new FormattedText(
            "Much longer line of text",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(_viewModel.FontFamily),
            _viewModel.FontSize,
            Brushes.Black).Width;

        Assert.Equal(expectedWidth, _viewModel.LongestLineWidth);
    }

    [AvaloniaFact]
    public void TotalHeight_ReturnsCorrectValue()
    {
        _mockTextBuffer.Setup(tb => tb.TotalHeight).Returns(500);
        Assert.Equal(500, _viewModel.TotalHeight);
    }

    [AvaloniaFact]
    public async Task CopyText_CopiesSelectedTextToClipboard()
    {
        _viewModel.SelectionStart = 0;
        _viewModel.SelectionEnd = 5;
        var selectedText = "Hello";
        _mockTextBuffer.Setup(tb => tb.GetText(0, 5)).Returns(selectedText);

        await _viewModel.CopyText();

        _mockClipboardService.Verify(cs => cs.SetTextAsync(selectedText), Times.Once);
    }

    [AvaloniaFact]
    public async Task PasteText_PastesTextFromClipboardAtCursorPosition()
    {
        var clipboardText = "World";
        _mockClipboardService.Setup(cs => cs.GetTextAsync()).ReturnsAsync(clipboardText);

        _viewModel.CursorPosition = 5;
        await _viewModel.PasteText();

        _mockTextBuffer.Verify(tb => tb.InsertText(5, clipboardText), Times.Once);
        Assert.Equal(10, _viewModel.CursorPosition);
    }
}