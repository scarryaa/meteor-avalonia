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
    [InlineData("Hello world", 6, 5)] // Previous word boundary from start of "world"
    [InlineData("Hello world", 11, 11)] // Previous word boundary from end of "world"
    public void GetPreviousWordBoundary_ReturnsCorrectBoundary(string text, int position, int expectedBoundary)
    {
        var textBufferMock = new Mock<ITextBuffer>();
        textBufferMock.Setup(tb => tb.Text).Returns(text);

        var result = _service.GetPreviousWordBoundary(textBufferMock.Object, position);

        Assert.Equal(expectedBoundary, result);
    }

    [Theory]
    [InlineData("Hello world", 0, 5)] // From start of "Hello" to end of "Hello"
    [InlineData("Hello world", 5, 5)] // From end of "Hello" to end of "Hello"
    [InlineData("Hello world", 6, 11)] // From start of "world" to end of "world"
    [InlineData("Hello   world", 5, 5)] // At end of word with multiple spaces
    [InlineData("Hello   world", 6, 8)] // In space between words
    [InlineData("Hello", 0, 5)] // Single word
    [InlineData("Hello", 5, 5)] // At the end of the text
    [InlineData("Hello_world", 0, 11)] // Underscore is considered part of the word
    [InlineData("Hello-world", 0, 11)] // Hyphen is considered part of the word
    [InlineData("  Hello  ", 0, 2)] // Leading spaces
    [InlineData("Hello123", 0, 8)] // Numbers are part of the word
    public void GetNextWordBoundary_ReturnsCorrectBoundary(string text, int position, int expectedBoundary)
    {
        var textBufferMock = new Mock<ITextBuffer>();
        textBufferMock.Setup(tb => tb.Text).Returns(text);

        var result = _service.GetNextWordBoundary(textBufferMock.Object, position);

        Assert.Equal(expectedBoundary, result);
    }
}