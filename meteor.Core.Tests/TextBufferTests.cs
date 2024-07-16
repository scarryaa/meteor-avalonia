using meteor.Core.Interfaces;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace meteor.Core.Tests;

public class TextBufferTests
{
    private readonly Mock<IRope> _mockRope;
    private readonly Mock<ILogger<Rope>> _mockLogger;
    private string _mockRopeText;
    private readonly List<int> _mockLineStarts;

    public TextBufferTests()
    {
        _mockRope = new Mock<IRope>();
        _mockLogger = new Mock<ILogger<Rope>>();
        _mockRopeText = string.Empty;
        _mockLineStarts = new List<int> { 0 };

        SetupMockRope();
    }

    private void SetupMockRope()
    {
        _mockRope.Setup(r => r.Length).Returns(() => _mockRopeText.Length);
        _mockRope.Setup(r => r.LineCount).Returns(() => _mockLineStarts.Count);
        _mockRope.Setup(r => r.GetText()).Returns(() => _mockRopeText);
        _mockRope.Setup(r => r.GetText(It.IsAny<int>(), It.IsAny<int>()))
            .Returns<int, int>((start, length) =>
                _mockRopeText.Substring(start, Math.Min(length, _mockRopeText.Length - start)));

        _mockRope.Setup(r => r.Insert(It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, string>((index, text) =>
            {
                _mockRopeText = _mockRopeText.Insert(index, text);
                UpdateLineStarts();
            });

        _mockRope.Setup(r => r.Delete(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((start, length) =>
            {
                _mockRopeText = _mockRopeText.Remove(start, Math.Min(length, _mockRopeText.Length - start));
                UpdateLineStarts();
            });

        _mockRope.Setup(r => r.GetLineStartPosition(It.IsAny<int>()))
            .Returns<int>(index => index >= 0 && index < _mockLineStarts.Count ? _mockLineStarts[index] : -1);

        _mockRope.Setup(r => r.GetLineEndPosition(It.IsAny<int>()))
            .Returns<int>(index =>
            {
                if (index < 0 || index >= _mockLineStarts.Count) return -1;
                return index == _mockLineStarts.Count - 1 ? _mockRopeText.Length : _mockLineStarts[index + 1] - 1;
            });

        _mockRope.Setup(r => r.GetLineIndexFromPosition(It.IsAny<int>()))
            .Returns<int>(position =>
            {
                if (position < 0 || position > _mockRopeText.Length) return -1;
                return _mockLineStarts.FindIndex(start => start > position) - 1;
            });

        _mockRope.Setup(r => r.GetLineText(It.IsAny<int>()))
            .Returns<int>(index =>
            {
                if (index < 0 || index >= _mockLineStarts.Count) return string.Empty;
                var start = _mockLineStarts[index];
                var end = index == _mockLineStarts.Count - 1 ? _mockRopeText.Length : _mockLineStarts[index + 1];
                return _mockRopeText.Substring(start, end - start);
            });
    }

    private void UpdateLineStarts()
    {
        _mockLineStarts.Clear();
        _mockLineStarts.Add(0);
        for (var i = 0; i < _mockRopeText.Length; i++)
            if (_mockRopeText[i] == '\n')
                _mockLineStarts.Add(i + 1);
    }
        
    [Fact]
    public void Constructor_ShouldInitializeEmptyBuffer()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        Assert.Equal("", buffer.Text);
        Assert.Equal(0, buffer.Length);
        Assert.Equal(1, buffer.LineCount);
    }

    [Fact]
    public void GetText_ShouldReturnCorrectSubstring()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Hello, World!");
        Assert.Equal("Hello", buffer.GetText(0, 5));
        Assert.Equal("World", buffer.GetText(7, 5));
    }

    [Fact]
    public void InsertText_ShouldInsertTextCorrectly()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.InsertText(0, "Hello");
        Assert.Equal("Hello", buffer.Text);
        buffer.InsertText(5, ", World!");
        Assert.Equal("Hello, World!", buffer.Text);
    }
    
    [Fact]
    public void InsertText_ShouldHandleMultilineInsertion()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.InsertText(0, "Line 1\nLine 2\nLine 3");
        Assert.Equal(3, buffer.LineCount);
        Assert.Equal("Line 1\nLine 2\nLine 3", buffer.Text);
    }

    [Fact]
    public void InsertText_ShouldUpdateLengthCorrectly()
    {
        // Arrange
        var mockRope = new Mock<IRope>();
        mockRope.SetupProperty(r => r.Length, 0);
        mockRope.Setup(r => r.Insert(It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, string>((index, text) => mockRope.Object.Length += text.Length);

        var buffer = new TextBuffer(mockRope.Object, _mockLogger.Object);

        // Act & Assert
        buffer.InsertText(0, "Hello");
        Assert.Equal(5, buffer.Length);

        buffer.InsertText(5, ", World!");
        Assert.Equal(13, buffer.Length);
    }


    [Fact]
    public void DeleteText_ShouldDeleteTextCorrectly()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Hello, World!");
        buffer.DeleteText(5, 2);
        Assert.Equal("HelloWorld!", buffer.Text);
    }

    [Fact]
    public void DeleteText_ShouldHandleMultilineDeletion()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        buffer.DeleteText(6, 7);
        Assert.Equal(2, buffer.LineCount);
        Assert.Equal("Line 1\nLine 3", buffer.Text);
    }

    [Fact]
    public void SetText_ShouldReplaceEntireContent()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Old Content");
        buffer.SetText("New Content");
        Assert.Equal("New Content", buffer.Text);
    }

    [Fact]
    public void Clear_ShouldRemoveAllContent()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Some Content");
        buffer.Clear();
        Assert.Equal("", buffer.Text);
        Assert.Equal(0, buffer.Length);
        Assert.Equal(1, buffer.LineCount);
    }

    [Fact]
    public void GetLineText_ShouldReturnCorrectLine()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal("Line 2", buffer.GetLineText(1));
    }

    [Fact]
    public void GetLineStartPosition_ShouldReturnCorrectPosition()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal(0, buffer.GetLineStartPosition(0));
        Assert.Equal(7, buffer.GetLineStartPosition(1));
        Assert.Equal(14, buffer.GetLineStartPosition(2));
    }

    [Fact]
    public void GetLineEndPosition_ShouldReturnCorrectPosition()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal(6, buffer.GetLineEndPosition(0));
        Assert.Equal(13, buffer.GetLineEndPosition(1));
        Assert.Equal(20, buffer.GetLineEndPosition(2));
    }

    [Fact]
    public void GetLineLength_ShouldReturnCorrectLength()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal(7, buffer.GetLineLength(0));
        Assert.Equal(7, buffer.GetLineLength(1));
        Assert.Equal(6, buffer.GetLineLength(2));
    }

    [Fact]
    public void GetLineIndexFromPosition_ShouldReturnCorrectIndex()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal(0, buffer.GetLineIndexFromPosition(0));
        Assert.Equal(1, buffer.GetLineIndexFromPosition(7));
        Assert.Equal(2, buffer.GetLineIndexFromPosition(14));
    }

    [Fact]
    public void TextChanged_ShouldRaiseEventOnModification()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        var eventRaised = false;
        buffer.TextChanged += (sender, args) => eventRaised = true;
        buffer.InsertText(0, "Hello");
        Assert.True(eventRaised);
    }

    [Fact]
    public void GetLineStarts_ShouldReturnCorrectStartPositions()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        var lineStarts = buffer.GetLineStarts();
        Assert.Equal(new[] { 0, 7, 14 }, lineStarts);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void GetLineText_ShouldReturnEmptyStringForInvalidIndex(int lineIndex)
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2");
        Assert.Equal(string.Empty, buffer.GetLineText(lineIndex));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void GetLineStartPosition_ShouldThrowForInvalidIndex(int lineIndex)
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2");
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetLineStartPosition(lineIndex));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void GetLineIndexFromPosition_ShouldThrowForInvalidPosition(int position)
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2");
    }

    [Fact]
    public void InsertText_AtEndOfBuffer_ShouldAppendCorrectly()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Hello");
        buffer.InsertText(5, " World");
        Assert.Equal("Hello World", buffer.Text);
    }

    [Fact]
    public void DeleteText_EntireBuffer_ShouldResultInEmptyBuffer()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Hello World");
        buffer.DeleteText(0, 11);
        Assert.Equal("", buffer.Text);
        Assert.Equal(0, buffer.Length);
        Assert.Equal(1, buffer.LineCount);
    }

    [Fact]
    public void InsertText_LargeText_ShouldUpdateLengthCorrectly()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        var largeText = new string('A', 1000000);
        buffer.InsertText(0, largeText);
        Assert.Equal(1000000, buffer.Length);
    }

    [Fact]
    public void GetLineIndexFromPosition_AtEndOfLine_ShouldReturnCorrectIndex()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal(0, buffer.GetLineIndexFromPosition(6)); // End of "Line 1"
        Assert.Equal(1, buffer.GetLineIndexFromPosition(13)); // End of "Line 2"
    }

    [Fact]
    public void GetLineLength_LastLine_ShouldReturnCorrectLength()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal(6, buffer.GetLineLength(2)); // "Line 3" without newline
    }

    [Fact]
    public void TextChanged_InsertText_ShouldProvideCorrectEventArgs()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        TextChangedEventArgs capturedArgs = null;
        buffer.TextChanged += (sender, args) => capturedArgs = args;

        buffer.InsertText(0, "Hello");

        Assert.NotNull(capturedArgs);
        Assert.Equal(0, capturedArgs.Position);
        Assert.Equal("Hello", capturedArgs.InsertedText);
        Assert.Equal(0, capturedArgs.DeletedLength);
    }

    [Fact]
    public void TextChanged_DeleteText_ShouldProvideCorrectEventArgs()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Hello World");
        TextChangedEventArgs capturedArgs = null;
        buffer.TextChanged += (sender, args) => capturedArgs = args;

        buffer.DeleteText(0, 6);

        Assert.NotNull(capturedArgs);
        Assert.Equal(0, capturedArgs.Position);
        Assert.Equal("", capturedArgs.InsertedText);
        Assert.Equal(6, capturedArgs.DeletedLength);
    }

    [Fact]
    public void GetLineText_WithBuffer_ShouldCopyCorrectTextToBuffer()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        var charBuffer = new char[10];

        var copiedLength = buffer.GetLineText(1, charBuffer);

        Assert.Equal(6, copiedLength);
        Assert.Equal("Line 2", new string(charBuffer, 0, copiedLength));
    }

    [Fact]
    public void GetLineText_WithSpan_ShouldCopyCorrectTextToSpan()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Line 1\nLine 2\nLine 3");
        var charArray = new char[10];
        var span = new Span<char>(charArray);

        var copiedLength = buffer.GetLineText(1, span);

        Assert.Equal(6, copiedLength);
        Assert.Equal("Line 2", new string(span.Slice(0, copiedLength)));
    }

    [Fact]
    public void Clear_ShouldTriggerTextChangedEvent()
    {
        var buffer = new TextBuffer(_mockRope.Object, _mockLogger.Object);
        buffer.SetText("Hello World");
        TextChangedEventArgs capturedArgs = null;
        buffer.TextChanged += (sender, args) => capturedArgs = args;

        buffer.Clear();

        Assert.NotNull(capturedArgs);
        Assert.Equal(0, capturedArgs.Position);
        Assert.Equal("", capturedArgs.InsertedText);
        Assert.Equal(11, capturedArgs.DeletedLength);
    }
}