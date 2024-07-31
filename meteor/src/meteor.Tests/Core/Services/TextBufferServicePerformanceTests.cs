using System.Diagnostics;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using meteor.Core.Services;
using Moq;

namespace meteor.Tests.Core.Services;

public class TextBufferServicePerformanceTests
{
    private readonly Mock<TextBuffer> _mockTextBuffer;
    private readonly Mock<ITextMeasurer> _mockTextMeasurer;
    private readonly TextBufferService _service;

    public TextBufferServicePerformanceTests()
    {
        _mockTextMeasurer = new Mock<ITextMeasurer>();
        _mockTextBuffer = new Mock<TextBuffer>();
        Mock<IEditorConfig> mockConfig = new();

        mockConfig.Setup(c => c.FontFamily).Returns("Arial");
        _service = new TextBufferService(_mockTextBuffer.Object, _mockTextMeasurer.Object, mockConfig.Object);
    }

    [Fact]
    public void InsertText_LargeTextPerformance_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var largeText = new string('a', 1000000); // 1 million characters
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        _service.InsertText(0, largeText);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 15,
            $"InsertText took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 15ms limit");
    }

    [Fact]
    public void DeleteText_LargeDeletePerformance_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var largeText = new string('a', 1000000); // 1 million characters
        _service.InsertText(0, largeText);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        _service.DeleteText(0, 1000000);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 15,
            $"DeleteText took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 15ms limit");
    }

    [Fact]
    public void GetLineIndexFromCharacterIndex_LargeDocumentPerformance_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var largeText = string.Join("\n", Enumerable.Repeat("a", 1000000)); // 1,000,000 lines
        _service.InsertText(0, largeText);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        for (var i = 0; i < 1000; i++) _service.GetLineIndexFromCharacterIndex(Random.Shared.Next(0, largeText.Length));
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1,
            $"1000 GetLineIndexFromCharacterIndex operations took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 1ms limit");
    }

    [Fact]
    public void GetMaxLineWidth_LargeDocumentPerformance_ShouldCompleteWithinTimeLimit()
    {
        // Arrange for 100,000 lines
        var largeText100K = string.Join("\n", Enumerable.Repeat("a", 100000)); // 100,000 lines
        _service.InsertText(0, largeText100K);
        _mockTextMeasurer.Setup(tm => tm.MeasureText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()))
            .Returns((10.0, 20.0));
        var stopwatch = new Stopwatch();

        // Act for 100,000 lines
        stopwatch.Start();
        _service.GetMaxLineWidth("Arial", 12);
        stopwatch.Stop();

        // Assert for 100,000 lines
        Assert.True(stopwatch.ElapsedMilliseconds < 125,
            $"GetMaxLineWidth for 100,000 lines took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 125ms limit");

        // Reset service and stopwatch for 1,000,000 lines case
        _service.DeleteText(0, largeText100K.Length);
        var largeText1M = string.Join("\n", Enumerable.Repeat("a", 1000000)); // 1,000,000 lines
        _service.InsertText(0, largeText1M);
        stopwatch.Reset();

        // Act for 1,000,000 lines
        stopwatch.Start();
        _service.GetMaxLineWidth("Arial", 12);
        stopwatch.Stop();

        // Assert for 1,000,000 lines
        Assert.True(stopwatch.ElapsedMilliseconds < 125,
            $"GetMaxLineWidth for 1,000,000 lines took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 125ms limit");
    }
}