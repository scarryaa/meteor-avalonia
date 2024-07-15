using meteor.Core.Models;

namespace meteor.Core.Tests;

public class TextBufferTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptyBuffer()
    {
        var buffer = new TextBuffer();
        Assert.Equal("", buffer.Text);
        Assert.Equal(0, buffer.Length);
        Assert.Equal(1, buffer.LineCount);
    }

    [Fact]
    public void GetText_ShouldReturnCorrectSubstring()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello, World!");
        Assert.Equal("Hello", buffer.GetText(0, 5));
        Assert.Equal("World", buffer.GetText(7, 5));
    }

    [Fact]
    public void InsertText_ShouldInsertTextCorrectly()
    {
        var buffer = new TextBuffer();
        buffer.InsertText(0, "Hello");
        Assert.Equal("Hello", buffer.Text);
        buffer.InsertText(5, ", World!");
        Assert.Equal("Hello, World!", buffer.Text);
    }

    [Fact]
    public void InsertText_ShouldHandleMultilineInsertion()
    {
        var buffer = new TextBuffer();
        buffer.InsertText(0, "Line 1\nLine 2\nLine 3");
        Assert.Equal(3, buffer.LineCount);
        Assert.Equal("Line 1", buffer.GetLineText(0));
        Assert.Equal("Line 2", buffer.GetLineText(1));
        Assert.Equal("Line 3", buffer.GetLineText(2));
    }

    [Fact]
    public void DeleteText_ShouldDeleteTextCorrectly()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello, World!");
        buffer.DeleteText(5, 2);
        Assert.Equal("HelloWorld!", buffer.Text);
    }

    [Fact]
    public void DeleteText_ShouldHandleMultilineDeletion()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Line 1\nLine 2\nLine 3");
        buffer.DeleteText(6, 7);
        Assert.Equal(2, buffer.LineCount);
        Assert.Equal("Line 1\nLine 3", buffer.Text);
    }

    [Fact]
    public void SetText_ShouldReplaceEntireContent()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Old Content");
        buffer.SetText("New Content");
        Assert.Equal("New Content", buffer.Text);
    }

    [Fact]
    public void Clear_ShouldRemoveAllContent()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Some Content");
        buffer.Clear();
        Assert.Equal("", buffer.Text);
        Assert.Equal(0, buffer.Length);
        Assert.Equal(1, buffer.LineCount);
    }

    [Fact]
    public void GetLineText_ShouldReturnCorrectLine()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal("Line 2", buffer.GetLineText(1));
    }

    [Fact]
    public void GetLineStartPosition_ShouldReturnCorrectPosition()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal(0, buffer.GetLineStartPosition(0));
        Assert.Equal(7, buffer.GetLineStartPosition(1));
        Assert.Equal(14, buffer.GetLineStartPosition(2));
    }

    [Fact]
    public void GetLineEndPosition_ShouldReturnCorrectPosition()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal(6, buffer.GetLineEndPosition(0));
        Assert.Equal(13, buffer.GetLineEndPosition(1));
        Assert.Equal(20, buffer.GetLineEndPosition(2));
    }

    [Fact]
    public void GetLineLength_ShouldReturnCorrectLength()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal(6, buffer.GetLineLength(0));
        Assert.Equal(6, buffer.GetLineLength(1));
        Assert.Equal(6, buffer.GetLineLength(2));
    }

    [Fact]
    public void GetLineIndexFromPosition_ShouldReturnCorrectIndex()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Line 1\nLine 2\nLine 3");
        Assert.Equal(0, buffer.GetLineIndexFromPosition(0));
        Assert.Equal(1, buffer.GetLineIndexFromPosition(7));
        Assert.Equal(2, buffer.GetLineIndexFromPosition(14));
    }

    [Fact]
    public void TextChanged_ShouldRaiseEventOnModification()
    {
        var buffer = new TextBuffer();
        var eventRaised = false;
        buffer.TextChanged += (sender, args) => eventRaised = true;
        buffer.InsertText(0, "Hello");
        Assert.True(eventRaised);
    }

    [Fact]
    public void GetLineStarts_ShouldReturnCorrectStartPositions()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Line 1\nLine 2\nLine 3");
        var lineStarts = buffer.GetLineStarts();
        Assert.Equal(new[] { 0, 7, 14 }, lineStarts);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void GetLineText_ShouldReturnEmptyStringForInvalidIndex(int lineIndex)
    {
        var buffer = new TextBuffer();
        buffer.SetText("Line 1\nLine 2");
        Assert.Equal(string.Empty, buffer.GetLineText(lineIndex));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void GetLineStartPosition_ShouldThrowForInvalidIndex(int lineIndex)
    {
        var buffer = new TextBuffer();
        buffer.SetText("Line 1\nLine 2");
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetLineStartPosition(lineIndex));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void GetLineIndexFromPosition_ShouldThrowForInvalidPosition(int position)
    {
        var buffer = new TextBuffer();
        buffer.SetText("Line 1\nLine 2");
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetLineIndexFromPosition(position));
    }
}