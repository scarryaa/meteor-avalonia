using meteor.Application.Services;
using meteor.Core.Interfaces.Services;
using Moq;

namespace meteor.UnitTests.Application.Services;

public class TextAnalysisServiceTests
{
    private readonly TextAnalysisService _textAnalysisService;
    private readonly Mock<ITextBufferService> _mockTextBuffer;

    public TextAnalysisServiceTests()
    {
        _textAnalysisService = new TextAnalysisService();
        _mockTextBuffer = new Mock<ITextBufferService>();
    }

    [Theory]
    [InlineData("Hello world", 0, 0, 5)]
    [InlineData("Hello world", 7, 6, 11)]
    [InlineData("Hello world", 5, 6, 11)]
    [InlineData("Hello123", 5, 0, 8)]
    [InlineData("   Hello   ", 0, 3, 8)] // Leading and trailing spaces
    [InlineData("Hello-World", 6, 6, 11)] // Hyphenated word
    [InlineData("123_abc", 2, 0, 7)] // Underscore in word
    [InlineData("", 0, 0, 0)] // Empty string
    [InlineData("Hello", 10, 5, 5)] // Index beyond text length
    public void GetWordBoundaries_ReturnsCorrectBoundaries(string text, int index, int expectedStart, int expectedEnd)
    {
        SetupMockTextBuffer(text);
        var (start, end) = _textAnalysisService.GetWordBoundaries(_mockTextBuffer.Object, index);
        Assert.Equal(expectedStart, start);
        Assert.Equal(expectedEnd, end);
    }
    
    [Theory]
    [InlineData("Line1\nLine2\nLine3", 0, 0, 5)]
    [InlineData("Line1\nLine2\nLine3", 6, 6, 11)]
    [InlineData("Line1\nLine2\nLine3", 12, 12, 17)]
    [InlineData("SingleLine", 5, 0, 10)]
    [InlineData("\n\n\n", 1, 1, 1)] // Empty lines
    [InlineData("Line1\r\nLine2", 7, 7, 12)] // Windows line endings
    [InlineData("", 0, 0, 0)] // Empty string
    public void GetLineBoundaries_ReturnsCorrectBoundaries(string text, int index, int expectedStart, int expectedEnd)
    {
        SetupMockTextBuffer(text);
        var (start, end) = _textAnalysisService.GetLineBoundaries(_mockTextBuffer.Object, index);
        Assert.Equal(expectedStart, start);
        Assert.Equal(expectedEnd, end);
    }

    [Theory]
    [InlineData("Line1\nLine2\nLine3", 2, 0)]
    [InlineData("Line1\nLine2\nLine3", 8, 2)]
    [InlineData("Line1\nLine2\nLine3", 14, 8)]
    [InlineData("SingleLine", 5, 0)]
    [InlineData("\n\n\n", 2, 1)] // Empty lines
    [InlineData("Line1\nLongerLine2\nLine3", 15, 5)] // Different line lengths
    [InlineData("Line1", 2, 0)] // First line
    [InlineData("", 0, 0)] // Empty string
    public void GetPositionAbove_ReturnsCorrectPosition(string text, int index, int expected)
    {
        SetupMockTextBuffer(text);
        var result = _textAnalysisService.GetPositionAbove(_mockTextBuffer.Object, index);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Line1\nLine2\nLine3", 2, 8)]
    [InlineData("Line1\nLine2\nLine3", 8, 14)]
    [InlineData("Line1\nLine2\nLine3", 14, 17)]
    [InlineData("SingleLine", 5, 10)]
    public void GetPositionBelow_ReturnsCorrectPosition(string text, int index, int expected)
    {
        SetupMockTextBuffer(text);
        var result = _textAnalysisService.GetPositionBelow(_mockTextBuffer.Object, index);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello world", 7, 6)]
    [InlineData("Hello123 world", 8, 0)]
    [InlineData("Hello world", 0, 0)]
    [InlineData("   Hello", 5, 3)]
    public void GetWordStart_ReturnsCorrectPosition(string text, int index, int expected)
    {
        SetupMockTextBuffer(text);
        var result = _textAnalysisService.GetWordStart(_mockTextBuffer.Object, index);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello world", 7, 11)]
    [InlineData("Hello123 world", 2, 8)]
    [InlineData("Hello world", 10, 11)]
    [InlineData("Hello   ", 2, 5)]
    public void GetWordEnd_ReturnsCorrectPosition(string text, int index, int expected)
    {
        SetupMockTextBuffer(text);
        var result = _textAnalysisService.GetWordEnd(_mockTextBuffer.Object, index);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Line1\nLine2\nLine3", 8, 6)]
    [InlineData("Line1\nLine2\nLine3", 0, 0)]
    [InlineData("SingleLine", 5, 0)]
    public void GetLineStart_ReturnsCorrectPosition(string text, int index, int expected)
    {
        SetupMockTextBuffer(text);
        var result = _textAnalysisService.GetLineStart(_mockTextBuffer.Object, index);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Line1\nLine2\nLine3", 8, 11)]
    [InlineData("Line1\nLine2\nLine3", 16, 17)]
    [InlineData("SingleLine", 5, 10)]
    public void GetLineEnd_ReturnsCorrectPosition(string text, int index, int expected)
    {
        SetupMockTextBuffer(text);
        var result = _textAnalysisService.GetLineEnd(_mockTextBuffer.Object, index);
        Assert.Equal(expected, result);
    }

    private void SetupMockTextBuffer(string text)
    {
        _mockTextBuffer.Setup(tb => tb.Length).Returns(text.Length);
        _mockTextBuffer.Setup(tb => tb[It.IsAny<int>()]).Returns((int i) => text[i]);
        _mockTextBuffer.Setup(tb => tb.IndexOf(It.IsAny<char>(), It.IsAny<int>()))
            .Returns((char c, int startIndex) => text.IndexOf(c, startIndex));
        _mockTextBuffer.Setup(tb => tb.LastIndexOf(It.IsAny<char>(), It.IsAny<int>()))
            .Returns((char c, int startIndex) => text.LastIndexOf(c, startIndex));
    }
}