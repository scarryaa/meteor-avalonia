using System.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using meteor.UI.Services;

namespace meteor.UnitTests.Application.Services;

public class AvaloniaTextMeasurerTests
{
    private readonly AvaloniaTextMeasurer _measurer;

    public AvaloniaTextMeasurerTests()
    {
        _measurer = new AvaloniaTextMeasurer(new Typeface("Arial"), 12);
    }

    [AvaloniaTheory]
    [InlineData("Hello", 0, 0, 0)]
    [InlineData("Hello", 100, 0, 5)]
    [InlineData("Hello\nWorld", 0, 15, 6)]
    public void GetIndexAtPosition_ReturnsCorrectIndex(string text, double x, double y, int expectedIndex)
    {
        var result = _measurer.GetIndexAtPosition(text, x, y);
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
        var firstResult = _measurer.GetIndexAtPosition(text, 50, 0);
        var secondResult = _measurer.GetIndexAtPosition(text, 50, 0);

        Assert.Equal(firstResult, secondResult);
    }
}