using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using meteor.Interfaces;
using meteor.ViewModels;
using Moq;

namespace tests.ViewModels;

public class ScrollableTextEditorViewModelTests
{
    private readonly Mock<ICursorPositionService> _mockCursorPositionService;
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly FontPropertiesViewModel _fontPropertiesViewModel;
    private readonly LineCountViewModel _lineCountViewModel;
    private readonly ScrollableTextEditorViewModel _viewModel;
    private readonly Mock<IClipboardService> _mockClipboardService;
    private readonly Mock<IThemeService> _mockThemeService;

    public ScrollableTextEditorViewModelTests()
    {
        _mockCursorPositionService = new Mock<ICursorPositionService>();
        _mockTextBuffer = new Mock<ITextBuffer>();
        _fontPropertiesViewModel = new FontPropertiesViewModel();
        _lineCountViewModel = new LineCountViewModel();
        _mockClipboardService = new Mock<IClipboardService>();
        ;
        _mockThemeService = new Mock<IThemeService>();
        ;

        _viewModel = new ScrollableTextEditorViewModel(
            _mockCursorPositionService.Object,
            _fontPropertiesViewModel,
            _lineCountViewModel,
            _mockTextBuffer.Object,
            _mockClipboardService.Object,
            _mockThemeService.Object
        );
    }

    [AvaloniaFact]
    public void Constructor_InitializesProperties()
    {
        Assert.NotNull(_viewModel.TextEditorViewModel);
        Assert.NotNull(_viewModel.LineCountViewModel);
        Assert.NotNull(_viewModel.GutterViewModel);
        Assert.NotNull(_viewModel.FontPropertiesViewModel);
    }

    [AvaloniaFact]
    public void VerticalOffset_UpdatesOffsetAndLineCountViewModel()
    {
        const double newOffset = 100;
        _viewModel.VerticalOffset = newOffset;

        Assert.Equal(newOffset, _viewModel.VerticalOffset);
        Assert.Equal(newOffset, _viewModel.Offset.Y);
        Assert.Equal(newOffset, _viewModel.LineCountViewModel.VerticalOffset);
    }

    [AvaloniaFact]
    public void HorizontalOffset_UpdatesOffset()
    {
        const double newOffset = 50;
        _viewModel.HorizontalOffset = newOffset;

        Assert.Equal(newOffset, _viewModel.HorizontalOffset);
        Assert.Equal(newOffset, _viewModel.Offset.X);
    }

    [AvaloniaFact]
    public void Viewport_UpdatesLineCountViewModelAndTotalHeight()
    {
        var newViewport = new Size(800, 600);
        _viewModel.Viewport = newViewport;

        Assert.Equal(newViewport, _viewModel.Viewport);
        Assert.Equal(newViewport.Height, _viewModel.LineCountViewModel.ViewportHeight);
    }

    [AvaloniaFact]
    public void UpdateLongestLineWidth_CalculatesCorrectly()
    {
        const int longestLineLength = 100;
        const double charWidth = 10;

        _mockTextBuffer.Setup(tb => tb.LongestLineLength).Returns(longestLineLength);
        _viewModel.TextEditorViewModel.CharWidth = charWidth;

        _viewModel.UpdateLongestLineWidth();

        Assert.Equal(longestLineLength * charWidth + 20, _viewModel.LongestLineWidth);
    }

    [AvaloniaFact]
    public void UpdateTotalHeight_SetsCorrectHeight()
    {
        const double totalHeight = 1000;
        _mockTextBuffer.Setup(tb => tb.TotalHeight).Returns(totalHeight);

        _viewModel.UpdateTotalHeight();

        Assert.Equal(totalHeight, _viewModel.TotalHeight);
    }

    [Fact]
    public void UpdateDimensions_CallsUpdateMethodsAndRaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, args) => propertyChangedEvents.Add(args.PropertyName);

        // Act
        _viewModel.UpdateDimensions();

        // Assert
        Assert.Contains(nameof(ScrollableTextEditorViewModel.Viewport), propertyChangedEvents);
        Assert.Contains(nameof(ScrollableTextEditorViewModel.Offset), propertyChangedEvents);
        Assert.Contains(nameof(ScrollableTextEditorViewModel.VerticalOffset), propertyChangedEvents);
        Assert.Contains(nameof(ScrollableTextEditorViewModel.HorizontalOffset), propertyChangedEvents);
    }

    [AvaloniaFact]
    public void FontFamily_UpdatesCorrectly()
    {
        var newFontFamily = new FontFamily("Arial");
        _viewModel.FontFamily = newFontFamily;

        Assert.Equal(newFontFamily, _viewModel.FontFamily);
        Assert.Equal(newFontFamily, _viewModel.FontPropertiesViewModel.FontFamily);
    }

    [AvaloniaFact]
    public void FontSize_UpdatesCorrectly()
    {
        const double newFontSize = 16;
        _viewModel.FontSize = newFontSize;

        Assert.Equal(newFontSize, _viewModel.FontSize);
        Assert.Equal(newFontSize, _viewModel.FontPropertiesViewModel.FontSize);
    }

    [Fact]
    public void LineHeight_UpdatesCorrectly()
    {
        // Arrange
        const double expectedLineHeight = 24;

        // Act
        _viewModel.LineHeight = expectedLineHeight;

        // Assert
        Assert.Equal(expectedLineHeight, _viewModel.LineHeight);
        _mockTextBuffer.VerifySet(tb => tb.LineHeight = expectedLineHeight);
    }

    [AvaloniaFact]
    public void WindowHeight_UpdatesTotalHeight()
    {
        const double newWindowHeight = 800;
        _viewModel.WindowHeight = newWindowHeight;

        Assert.Equal(newWindowHeight, _viewModel.WindowHeight);
        Assert.Equal(newWindowHeight, _viewModel.TotalHeight);
    }

    [AvaloniaFact]
    public void WindowWidth_UpdatesLongestLineWidth()
    {
        const double newWindowWidth = 1200;
        _viewModel.WindowWidth = newWindowWidth;

        Assert.Equal(newWindowWidth, _viewModel.WindowWidth);
        Assert.Equal(newWindowWidth + 20, _viewModel.LongestLineWidth);
    }

    [Fact]
    public void UpdateViewProperties_RaisesPropertyChangedForAllProperties()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, args) => propertyChangedEvents.Add(args.PropertyName);

        var textEditorPropertyChangedEvents = new List<string>();
        _viewModel.TextEditorViewModel.PropertyChanged +=
            (sender, args) => textEditorPropertyChangedEvents.Add(args.PropertyName);

        // Act
        _viewModel.UpdateViewProperties();

        // Assert
        Assert.Contains(nameof(ScrollableTextEditorViewModel.FontFamily), propertyChangedEvents);
        Assert.Contains(nameof(ScrollableTextEditorViewModel.FontSize), propertyChangedEvents);
        Assert.Contains(nameof(ScrollableTextEditorViewModel.LineHeight), propertyChangedEvents);
        Assert.Contains(nameof(ScrollableTextEditorViewModel.LongestLineWidth), propertyChangedEvents);
        Assert.Contains(nameof(ScrollableTextEditorViewModel.VerticalOffset), propertyChangedEvents);
        Assert.Contains(nameof(ScrollableTextEditorViewModel.HorizontalOffset), propertyChangedEvents);
        Assert.Contains(nameof(ScrollableTextEditorViewModel.Offset), propertyChangedEvents);
        Assert.Contains(nameof(ScrollableTextEditorViewModel.Viewport), propertyChangedEvents);

        Assert.Contains(nameof(TextEditorViewModel.FontFamily), textEditorPropertyChangedEvents);
        Assert.Contains(nameof(TextEditorViewModel.FontSize), textEditorPropertyChangedEvents);
        Assert.Contains(nameof(TextEditorViewModel.LineHeight), textEditorPropertyChangedEvents);
        Assert.Contains(nameof(TextEditorViewModel.WindowHeight), textEditorPropertyChangedEvents);
        Assert.Contains(nameof(TextEditorViewModel.WindowWidth), textEditorPropertyChangedEvents);
        Assert.Contains(nameof(TextEditorViewModel.TextBuffer), textEditorPropertyChangedEvents);
        Assert.Contains(nameof(TextEditorViewModel.TotalHeight), textEditorPropertyChangedEvents);

        _mockTextBuffer.Verify(tb => tb.UpdateLineCache(), Times.Once);
    }
}