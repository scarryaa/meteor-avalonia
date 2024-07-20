using System.Diagnostics;
using System.Text;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using meteor.Core.Interfaces.Services;
using meteor.UI.Services;
using Moq;

namespace meteor.UnitTests.Application.Services;

public class AvaloniaTextMeasurerTests
{
    private readonly AvaloniaTextMeasurer _measurer;
    private readonly Mock<ITextBufferService> _textBufferService;

    public AvaloniaTextMeasurerTests()
    {
        _textBufferService = new Mock<ITextBufferService>();
        _measurer = new AvaloniaTextMeasurer(new Typeface("Arial"), 12);
    }

    [AvaloniaTheory]
    [InlineData("Hello", 0, 0, 0)]
    [InlineData("Hello", 100, 0, 5)]
    [InlineData("Hello\nWorld", 0, 15, 6)]
    [InlineData("Line1\nLine2\nLine3", 0, 25, 12)]
    [InlineData("LongWord", 3.5, 0, 0)]
    [InlineData("Short", 100, 100, 5)]
    [InlineData("", 10, 10, 0)]
    public void GetIndexAtPosition_ReturnsCorrectIndex(string text, double x, double y, int expectedIndex)
    {
        // Arrange
        _textBufferService.Setup(s => s.Length).Returns(text.Length);
        _textBufferService.Setup(s => s.IndexOf('\n', It.IsAny<int>()))
            .Returns((char c, int startIndex) => text.IndexOf('\n', startIndex));
        _textBufferService.Setup(s => s.GetTextSegment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<StringBuilder>()))
            .Callback((int start, int length, StringBuilder sb) =>
            {
                sb.Clear();
                sb.Append(text.Substring(start, Math.Min(length, text.Length - start)));
            });

        // Act
        var result = _measurer.GetIndexAtPosition(_textBufferService.Object, x, y, 0, 0);

        // Assert
        Assert.Equal(expectedIndex, result);
    }

    [AvaloniaTheory]
    [InlineData("Line1\nLine2\nLine3", 0, 25, 12)] // Position on third line
    [InlineData("LongWord", 3.5, 0, 0)] // Position between characters
    [InlineData("Short", 100, 100, 5)] // Position beyond end of text
    [InlineData("", 10, 10, 0)] // Empty text buffer
    public void GetIndexAtPosition_AdditionalScenarios(string text, double x, double y, int expectedIndex)
    {
        _textBufferService.Setup(s => s.Length).Returns(text.Length);
        _textBufferService.Setup(s => s.IndexOf('\n', It.IsAny<int>()))
            .Returns((char c, int startIndex) => text.IndexOf('\n', startIndex));
        _textBufferService.Setup(s => s.GetTextSegment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<StringBuilder>()))
            .Callback((int start, int length, StringBuilder sb) =>
            {
                sb.Clear();
                sb.Append(text.Substring(start, Math.Min(length, text.Length - start)));
            });

        var result = _measurer.GetIndexAtPosition(_textBufferService.Object, x, y, 0, 0);
        Assert.Equal(expectedIndex, result);
    }

    [AvaloniaTheory]
    [InlineData("Hello", 0, 0, 0)]
    [InlineData("Hello", 4, 30, 0)]
    [InlineData("Hello\nWorld", 6, 0, 12)]
    public void GetPositionAtIndex_ReturnsCorrectPosition(string text, int index, double expectedX, double expectedY)
    {
        var (x, y) = _measurer.GetPositionAtIndex(text, index);
        Assert.InRange(x, expectedX - 10, expectedX + 10);
        Assert.InRange(y, expectedY - 10, expectedY + 10);
    }

    [AvaloniaTheory]
    [InlineData("", 0)]
    [InlineData("A", 7, 5)]
    [InlineData("Hello", 50, 3)]
    public void GetStringWidth_ReturnsCorrectWidth(string text, double expectedWidth, double tolerance = 0.1)
    {
        var width = _measurer.GetStringWidth(text);
        Assert.InRange(width, expectedWidth - tolerance, expectedWidth + tolerance);
    }

    [AvaloniaFact]
    public void CacheImprovesPerformance()
    {
        const string text = "Performance Test";
        _measurer.ClearCache();

        var watch = Stopwatch.StartNew();
        _measurer.GetStringWidth(text);
        var firstRunTime = watch.ElapsedTicks;

        watch.Restart();
        _measurer.GetStringWidth(text);
        var secondRunTime = watch.ElapsedTicks;

        Assert.True(secondRunTime < firstRunTime);
    }

    [AvaloniaFact]
    public void ConsistentResultsAcrossMultipleCalls()
    {
        const string text = "Consistency Test";
        var firstResult = _measurer.GetIndexAtPosition(_textBufferService.Object, 50, 0, 0, 0);
        var secondResult = _measurer.GetIndexAtPosition(_textBufferService.Object, 50, 0, 0, 0);

        Assert.Equal(firstResult, secondResult);
    }
}