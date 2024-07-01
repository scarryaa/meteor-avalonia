using Avalonia.Headless.XUnit;
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

    [Fact]
    public void Constructor_InitializesProperties()
    {
        Assert.NotNull(_viewModel.FontPropertiesViewModel);
        Assert.Equal(_mockTextBuffer.Object, _viewModel.TextBuffer);
    }

    [Fact]
    public void CursorPosition_UpdatesAndNotifiesService()
    {
        long newPosition = 10;
        _viewModel.CursorPosition = newPosition;

        Assert.Equal(newPosition, _viewModel.CursorPosition);
        _mockCursorPositionService.Verify(s => s.UpdateCursorPosition(newPosition, It.IsAny<List<long>>()), Times.Once);
    }

    [Fact]
    public void SelectionStart_UpdatesAndNotifiesChange()
    {
        long newSelection = 5;
        var eventRaised = false;
        _viewModel.SelectionChanged += (s, e) => eventRaised = true;

        _viewModel.SelectionStart = newSelection;

        Assert.Equal(newSelection, _viewModel.SelectionStart);
        Assert.True(eventRaised);
    }

    [Fact]
    public void SelectionEnd_UpdatesAndNotifiesChange()
    {
        long newSelection = 15;
        var eventRaised = false;
        _viewModel.SelectionChanged += (s, e) => eventRaised = true;

        _viewModel.SelectionEnd = newSelection;

        Assert.Equal(newSelection, _viewModel.SelectionEnd);
        Assert.True(eventRaised);
    }

    [Fact]
    public void InsertText_UpdatesTextBufferAndCursorPosition()
    {
        long position = 5;
        var text = "Hello";
        _viewModel.InsertText(position, text);

        _mockTextBuffer.Verify(tb => tb.InsertText(position, text), Times.Once);
        Assert.Equal(position + text.Length, _viewModel.CursorPosition);
    }

    [Fact]
    public void DeleteText_CallsTextBufferDelete()
    {
        long start = 5;
        long length = 3;
        _viewModel.DeleteText(start, length);

        _mockTextBuffer.Verify(tb => tb.DeleteText(start, length), Times.Once);
    }

    [Fact]
    public void ClearSelection_SetsBothSelectionPointsToCursorPosition()
    {
        _viewModel.CursorPosition = 10;
        _viewModel.SelectionStart = 5;
        _viewModel.SelectionEnd = 15;

        _viewModel.ClearSelection();

        Assert.Equal(_viewModel.CursorPosition, _viewModel.SelectionStart);
        Assert.Equal(_viewModel.CursorPosition, _viewModel.SelectionEnd);
    }

    [Fact]
    public void UpdateLineStarts_CallsTextBufferUpdateLineCache()
    {
        // Reset the mock to clear the call from the constructor
        _mockTextBuffer.Invocations.Clear();

        _viewModel.UpdateLineStarts();

        _mockTextBuffer.Verify(tb => tb.UpdateLineCache(), Times.Once);
    }

    [Fact]
    public void WindowHeight_UpdatesPropertyCorrectly()
    {
        var newHeight = 100.5;
        _viewModel.WindowHeight = newHeight;

        Assert.Equal(newHeight, _viewModel.WindowHeight);
    }

    [Fact]
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

    [Fact]
    public void Focus_InvokesRequestFocusEvent()
    {
        var eventRaised = false;
        _viewModel.RequestFocus += (s, e) => eventRaised = true;

        _viewModel.Focus();

        Assert.True(eventRaised);
    }

    [Fact]
    public void OnInvalidateRequired_InvokesInvalidateRequiredEvent()
    {
        var eventRaised = false;
        _viewModel.InvalidateRequired += (s, e) => eventRaised = true;

        _viewModel.OnInvalidateRequired();

        Assert.True(eventRaised);
    }
}