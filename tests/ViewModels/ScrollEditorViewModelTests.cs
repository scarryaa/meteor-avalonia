using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using meteor.Interfaces;
using meteor.ViewModels;
using Moq;

namespace meteor.Tests.ViewModels;

public class ScrollableTextEditorViewModelTests
{
    private readonly Mock<ICursorPositionService> _mockCursorPositionService;
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly FontPropertiesViewModel _fontPropertiesViewModel;
    private readonly LineCountViewModel _lineCountViewModel;
    private readonly ScrollableTextEditorViewModel _viewModel;
    private readonly Mock<IClipboardService> _mockClipboardService;

    public ScrollableTextEditorViewModelTests()
    {
        _mockCursorPositionService = new Mock<ICursorPositionService>();
        _mockTextBuffer = new Mock<ITextBuffer>();
        _fontPropertiesViewModel = new FontPropertiesViewModel();
        _lineCountViewModel = new LineCountViewModel();
        _mockClipboardService = new Mock<IClipboardService>();

        _viewModel = new ScrollableTextEditorViewModel(
            _mockCursorPositionService.Object,
            _fontPropertiesViewModel,
            _lineCountViewModel,
            _mockTextBuffer.Object,
            _mockClipboardService.Object
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

        Assert.Equal(longestLineLength * charWidth, _viewModel.LongestLineWidth);
    }

    [AvaloniaFact]
    public void UpdateTotalHeight_SetsCorrectHeight()
    {
        const double totalHeight = 1000;
        _mockTextBuffer.Setup(tb => tb.TotalHeight).Returns(totalHeight);

        _viewModel.UpdateTotalHeight();

        Assert.Equal(totalHeight, _viewModel.TotalHeight);
    }

    [AvaloniaFact]
    public void UpdateDimensions_CallsUpdateMethodsAndRaisesPropertyChanged()
    {
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ScrollableTextEditorViewModel.LongestLineWidth) ||
                args.PropertyName == nameof(ScrollableTextEditorViewModel.TotalHeight))
                propertyChangedRaised = true;
        };

        _viewModel.UpdateDimensions();

        Assert.True(propertyChangedRaised);
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

    [AvaloniaFact]
    public void LineHeight_UpdatesCorrectly()
    {
        const double newLineHeight = 24;
        _viewModel.LineHeight = newLineHeight;

        Assert.Equal(newLineHeight, _viewModel.LineHeight);
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
        Assert.Equal(newWindowWidth, _viewModel.LongestLineWidth);
    }

    [AvaloniaFact]
    public void UpdateViewProperties_RaisesPropertyChangedForAllProperties()
    {
        var raisedProperties = new HashSet<string>();
        _viewModel.PropertyChanged += (sender, args) => raisedProperties.Add(args.PropertyName);

        _viewModel.UpdateViewProperties();

        var expectedProperties = new[]
        {
            nameof(ScrollableTextEditorViewModel.FontFamily),
            nameof(ScrollableTextEditorViewModel.FontSize),
            nameof(ScrollableTextEditorViewModel.LineHeight),
            nameof(ScrollableTextEditorViewModel.LongestLineWidth),
            nameof(ScrollableTextEditorViewModel.VerticalOffset),
            nameof(ScrollableTextEditorViewModel.HorizontalOffset),
            nameof(ScrollableTextEditorViewModel.Offset),
            nameof(ScrollableTextEditorViewModel.Viewport)
        };

        foreach (var prop in expectedProperties) Assert.Contains(prop, raisedProperties);

        _mockTextBuffer.Verify(tb => tb.UpdateLineCache(), Times.Once);
    }
}