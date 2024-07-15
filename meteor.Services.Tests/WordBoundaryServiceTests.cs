using meteor.Core.Interfaces;
using Moq;

namespace meteor.Services.Tests;

public class WordBoundaryServiceTests
{
    private readonly WordBoundaryService _service;

    public WordBoundaryServiceTests()
    {
        _service = new WordBoundaryService();
    }

    [Theory]
    [InlineData("Hello world", 6, 6, 11)] // Word boundary at the start of "world"
    [InlineData("Hello world", 0, 0, 5)] // Word boundary at the start of "Hello"
    [InlineData("Hello world", 5, 0, 5)] // Word boundary at the end of "Hello"
    public void GetWordBoundaries_ReturnsCorrectBoundaries(string text, int position, int expectedStart,
        int expectedEnd)
    {
        var textBufferMock = new Mock<ITextBuffer>();
        textBufferMock.Setup(tb => tb.Text).Returns(text);

        var result = _service.GetWordBoundaries(textBufferMock.Object, position);

        Assert.Equal(expectedStart, result.start);
        Assert.Equal(expectedEnd, result.end);
    }

    [Theory]
    [InlineData("Hello world", 6, 0)] // Previous word boundary from start of "world"
    [InlineData("Hello world", 11, 6)] // Previous word boundary from end of "world"
    public void GetPreviousWordBoundary_ReturnsCorrectBoundary(string text, int position, int expectedBoundary)
    {
        var textBufferMock = new Mock<ITextBuffer>();
        textBufferMock.Setup(tb => tb.Text).Returns(text);

        var result = _service.GetPreviousWordBoundary(textBufferMock.Object, position);

        Assert.Equal(expectedBoundary, result);
    }

    [Theory]
    [InlineData("Hello world", 5, 6)] // Next word boundary from end of "Hello"
    [InlineData("Hello world", 0, 5)] // Next word boundary from start of "Hello"
    public void GetNextWordBoundary_ReturnsCorrectBoundary(string text, int position, int expectedBoundary)
    {
        var textBufferMock = new Mock<ITextBuffer>();
        textBufferMock.Setup(tb => tb.Text).Returns(text);

        var result = _service.GetNextWordBoundary(textBufferMock.Object, position);

        Assert.Equal(expectedBoundary, result);
    }
}