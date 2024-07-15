using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Contexts;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Rendering;
using meteor.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace meteor.Core.Tests;

public class RenderManagerTests
{
    private readonly Mock<ITextEditorContext> _mockContext;
    private readonly Mock<IThemeService> _mockThemeService;
    private readonly Mock<ISyntaxHighlighter> _mockSyntaxHighlighter;
    private readonly Mock<ITextMeasurer> _mockTextMeasurer;
    private readonly Mock<ILogger<RenderManager>> _mockLogger;
    private readonly Mock<IFontFamily> _mockFontFamily;
    private readonly Mock<ITextEditorViewModel> _mockViewModel;
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly RenderManager _renderManager;

    public RenderManagerTests()
    {
        _mockContext = new Mock<ITextEditorContext>();
        _mockThemeService = new Mock<IThemeService>();
        _mockSyntaxHighlighter = new Mock<ISyntaxHighlighter>();
        _mockTextMeasurer = new Mock<ITextMeasurer>();
        _mockLogger = new Mock<ILogger<RenderManager>>();
        _mockFontFamily = new Mock<IFontFamily>();
        _mockViewModel = new Mock<ITextEditorViewModel>();
        _mockTextBuffer = new Mock<ITextBuffer>();

        _mockContext.SetupGet(c => c.TextEditorViewModel).Returns(_mockViewModel.Object);
        _mockViewModel.SetupGet(vm => vm.TextBuffer).Returns(_mockTextBuffer.Object);

        _renderManager = new RenderManager(
            _mockContext.Object,
            _mockThemeService.Object,
            () => _mockSyntaxHighlighter.Object,
            _mockTextMeasurer.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void UpdateFilePath_ShouldUpdateFilePathAndTriggerSyntaxHighlighting()
    {
        // Arrange
        var mockViewModel = new Mock<ITextEditorViewModel>();
        var mockTextBuffer = new Mock<ITextBuffer>();
        mockViewModel.SetupGet(vm => vm.TextBuffer).Returns(mockTextBuffer.Object);
        _mockContext.SetupGet(c => c.TextEditorViewModel).Returns(mockViewModel.Object);

        // Act
        _renderManager.UpdateFilePath("test.cs");

        // Assert
        mockTextBuffer.Verify(tb => tb.GetText(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void AttachToViewModel_ShouldSetTextEditorViewModel()
    {
        // Arrange
        var mockViewModel = new Mock<ITextEditorViewModel>();

        // Act
        _renderManager.AttachToViewModel(mockViewModel.Object);

        // Assert
        _mockContext.VerifySet(c => c.TextEditorViewModel = mockViewModel.Object, Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldUpdateSyntaxHighlightingAndInvalidate()
    {
        // Arrange
        var mockViewModel = new Mock<ITextEditorViewModel>();
        _mockContext.SetupGet(c => c.TextEditorViewModel).Returns(mockViewModel.Object);

        // Act
        await _renderManager.InitializeAsync("initial text");

        // Assert
        mockViewModel.Verify(vm => vm.OnInvalidateRequired(), Times.Exactly(2));
    }

    [Fact]
    public void Render_ShouldCallAllRenderMethods()
    {
        // Arrange
        var mockDrawingContext = new Mock<IDrawingContext>();
        _mockContext.SetupGet(c => c.BackgroundBrush).Returns(Mock.Of<IBrush>());
        _mockContext.SetupGet(c => c.SelectionBrush).Returns(Mock.Of<IBrush>());
        _mockContext.SetupGet(c => c.CursorBrush).Returns(Mock.Of<IBrush>());
        _mockContext.SetupGet(c => c.LineHeight).Returns(20);
        _mockContext.SetupGet(c => c.FontSize).Returns(12);
        _mockContext.SetupGet(c => c.FontFamily).Returns(_mockFontFamily.Object);
        _mockContext.SetupGet(c => c.ForegroundBrush).Returns(Mock.Of<IBrush>());
        _mockFontFamily.SetupGet(ff => ff.Name).Returns("Arial");
        _mockViewModel.SetupGet(vm => vm.RequiredWidth).Returns(100);
        _mockViewModel.SetupGet(vm => vm.SelectionStart).Returns(0);
        _mockViewModel.SetupGet(vm => vm.SelectionEnd).Returns(0);
        _mockViewModel.SetupGet(vm => vm.CursorPosition).Returns(0);
        _mockTextBuffer.Setup(tb => tb.GetLineText(It.IsAny<int>())).Returns("Sample text");
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(It.IsAny<int>())).Returns(0);
        _mockTextBuffer.Setup(tb => tb.GetLineStartPosition(It.IsAny<int>())).Returns(0);
        _mockTextBuffer.Setup(tb => tb.GetLineLength(It.IsAny<int>())).Returns(11);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(11);
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(1);
        _mockTextMeasurer.Setup(tm => tm.MeasureWidth(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<string>()))
            .Returns(50);

        // Act
        _renderManager.Render(mockDrawingContext.Object, 0, 100);

        // Assert
        mockDrawingContext.Verify(dc => dc.FillRectangle(It.IsAny<IBrush>(), It.IsAny<Rect>()), Times.AtLeastOnce);
        mockDrawingContext.Verify(dc => dc.DrawText(It.IsAny<IFormattedText>(), It.IsAny<Point>()), Times.AtLeastOnce);
        mockDrawingContext.Verify(dc => dc.DrawLine(It.IsAny<IPen>(), It.IsAny<Point>(), It.IsAny<Point>()),
            Times.Once);
    }

    [Fact]
    public void MeasureSelection_ShouldHandleNullLineText()
    {
        // Arrange
        var mockViewModel = new Mock<ITextEditorViewModel>();
        var mockTextBuffer = new Mock<ITextBuffer>();
        _mockContext.SetupGet(c => c.TextEditorViewModel).Returns(mockViewModel.Object);
        _mockContext.SetupGet(c => c.FontSize).Returns(12);
        _mockContext.SetupGet(c => c.FontFamily).Returns(_mockFontFamily.Object);
        _mockFontFamily.SetupGet(ff => ff.Name).Returns("Arial");
        mockViewModel.SetupGet(vm => vm.TextBuffer).Returns(mockTextBuffer.Object);
        mockTextBuffer.Setup(tb => tb.GetLineText(It.IsAny<int>())).Returns((string)null);
        _mockTextMeasurer.Setup(tm => tm.MeasureWidth(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<string>()))
            .Returns(0);

        // Act
        var result = _renderManager.MeasureSelection(null, 0, 5);

        // Assert
        Assert.Equal(0, result.xStart);
        Assert.Equal(0, result.xEnd);
    }

    [Fact]
    public void UpdateContext_ShouldUpdateContextAndInvalidateLines()
    {
        // Arrange
        var newMockContext = new Mock<ITextEditorContext>();
        var mockViewModel = new Mock<ITextEditorViewModel>();
        var mockTextBuffer = new Mock<ITextBuffer>();
        mockViewModel.SetupGet(vm => vm.TextBuffer).Returns(mockTextBuffer.Object);
        newMockContext.SetupGet(c => c.TextEditorViewModel).Returns(mockViewModel.Object);

        // Act
        _renderManager.UpdateContext(newMockContext.Object);

        // Assert
        mockTextBuffer.Verify(tb => tb.GetText(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        mockViewModel.Verify(vm => vm.OnInvalidateRequired(), Times.Once);
    }

    [Fact]
    public void InvalidateLine_ShouldRemoveLineFromCache()
    {
        // Arrange
        var lineIndex = 0;

        // Act
        _renderManager.InvalidateLine(lineIndex);

        // Assert
        // This test is limited as we can't directly verify the cache operation
    }

    [Fact]
    public void InvalidateLines_ShouldInvalidateRangeAndTriggerInvalidation()
    {
        // Arrange
        var mockViewModel = new Mock<ITextEditorViewModel>();
        _mockContext.SetupGet(c => c.TextEditorViewModel).Returns(mockViewModel.Object);

        // Act
        _renderManager.InvalidateLines(0, 5);

        // Assert
        mockViewModel.Verify(vm => vm.OnInvalidateRequired(), Times.Once);
    }

    [Fact]
    public async Task UpdateSyntaxHighlightingAsync_ShouldHighlightSyntaxAndInvalidateLines()
    {
        // Arrange
        var text = "test code";
        _mockSyntaxHighlighter.Setup(sh =>
                sh.HighlightSyntax(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(new List<SyntaxToken>());
        _mockTextBuffer.Setup(tb => tb.Length).Returns(text.Length);

        // Act
        await _renderManager.UpdateSyntaxHighlightingAsync(text);

        // Assert
        _mockSyntaxHighlighter.Verify(
            sh => sh.HighlightSyntax(text, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        _mockViewModel.Verify(vm => vm.OnInvalidateRequired(), Times.Once);
    }
}