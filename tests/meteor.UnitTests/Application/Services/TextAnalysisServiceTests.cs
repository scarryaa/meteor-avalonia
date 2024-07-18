using meteor.Application.Services;

namespace meteor.UnitTests.Application.Services;

public class TextAnalysisServiceTests
{
    private readonly TextAnalysisService _textAnalysisService;

    public TextAnalysisServiceTests()
    {
        _textAnalysisService = new TextAnalysisService();
    }

    [Theory]
    [InlineData("Hello world", 0, 0, 5)] // Start of "Hello"
    [InlineData("Hello world", 7, 6, 11)] // Start of "world"
    [InlineData("Hello world", 5, 5, 5)] // Space between words
    [InlineData("Hello123", 5, 0, 8)] // Mixed alphanumeric
    public void GetWordBoundariesAt_ReturnsCorrectBoundaries(string text, int index, int expectedStart, int expectedEnd)
    {
        var (start, end) = _textAnalysisService.GetWordBoundariesAt(text, index);
        Assert.Equal(expectedStart, start);
        Assert.Equal(expectedEnd, end);
    }

    [Theory]
    [InlineData("Line1\nLine2\nLine3", 0, 0, 5)] // First line
    [InlineData("Line1\nLine2\nLine3", 6, 6, 11)] // Second line
    [InlineData("Line1\nLine2\nLine3", 12, 12, 17)] // Third line
    [InlineData("SingleLine", 5, 0, 10)] // Single line without newline
    public void GetLineBoundariesAt_ReturnsCorrectBoundaries(string text, int index, int expectedStart, int expectedEnd)
    {
        var (start, end) = _textAnalysisService.GetLineBoundariesAt(text, index);
        Assert.Equal(expectedStart, start);
        Assert.Equal(expectedEnd, end);
    }

    [Theory]
    [InlineData("Line1\nLine2\nLine3", 2, 0)] // First line to first line
    [InlineData("Line1\nLine2\nLine3", 8, 2)] // Second line to first line
    [InlineData("Line1\nLine2\nLine3", 14, 8)] // Third line to second line
    [InlineData("SingleLine", 5, 0)] // Single line
    public void GetPositionAbove_ReturnsCorrectPosition(string text, int index, int expected)
    {
        var result = _textAnalysisService.GetPositionAbove(text, index);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Line1\nLine2\nLine3", 2, 8)] // First line to second line
    [InlineData("Line1\nLine2\nLine3", 8, 14)] // Second line to third line
    [InlineData("Line1\nLine2\nLine3", 14, 17)] // Third line to end of text
    [InlineData("SingleLine", 5, 10)] // Single line
    public void GetPositionBelow_ReturnsCorrectPosition(string text, int index, int expected)
    {
        var result = _textAnalysisService.GetPositionBelow(text, index);
        Assert.Equal(expected, result);
    }
}