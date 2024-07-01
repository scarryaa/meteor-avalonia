namespace tests.Models;

public class TextBufferTests
{
    [Fact]
    public void SetText_ShouldUpdateTextBuffer()
    {
        var buffer = new TextBuffer();
        var newText = "Hello\nWorld";

        buffer.SetText(newText);

        Assert.Equal(newText, buffer.Text);
        Assert.Equal(2, buffer.LineCount);
        Assert.Equal(newText.Length, buffer.Length);
        Assert.Equal("Hello\n", buffer.GetLineText(0));
        Assert.Equal("World", buffer.GetLineText(1));
    }

    [Fact]
    public void Clear_ShouldEmptyTextBuffer()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello\nWorld");

        buffer.Clear();

        Assert.Equal(string.Empty, buffer.Text);
        Assert.Equal(1, buffer.LineCount); // Even after clearing, there should be one empty line
        Assert.Equal(0, buffer.Length);
    }

    [Fact]
    public void InsertText_ShouldInsertTextAtPosition()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello World");

        buffer.InsertText(6, "Beautiful ");

        Assert.Equal("Hello Beautiful World", buffer.Text);
        Assert.Equal(1, buffer.LineCount);
        Assert.Equal(21, buffer.Length); // Updated expected length
    }

    [Fact]
    public void DeleteText_ShouldRemoveTextFromPosition()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello Beautiful World");

        buffer.DeleteText(6, 10);

        Assert.Equal("Hello World", buffer.Text);
        Assert.Equal(1, buffer.LineCount);
        Assert.Equal(11, buffer.Length);
    }

    [Fact]
    public void GetLineText_ShouldReturnCorrectLineText()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello\nWorld");

        Assert.Equal("Hello\n", buffer.GetLineText(0));
        Assert.Equal("World", buffer.GetLineText(1));
    }

    [Fact]
    public void GetLineStartPosition_ShouldReturnCorrectStartPosition()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello\nWorld");

        Assert.Equal(0, buffer.GetLineStartPosition(0));
        Assert.Equal(6, buffer.GetLineStartPosition(1));
    }

    [Fact]
    public void GetLineEndPosition_ShouldReturnCorrectEndPosition()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello\nWorld");

        Assert.Equal(5, buffer.GetLineEndPosition(0));
        Assert.Equal(10, buffer.GetLineEndPosition(1)); // Updated expected position
    }

    [Fact]
    public void UpdateLineCache_ShouldUpdateLineStartsAndLengths()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello\nWorld");

        Assert.Equal(2, buffer.LineStarts.Count);
        Assert.Equal(6, buffer.GetLineLength(0)); // Length includes newline character
        Assert.Equal(5, buffer.GetLineLength(1));
    }

    [Fact]
    public void GetLineIndexFromPosition_ShouldReturnCorrectLineIndex()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello\nWorld");

        Assert.Equal(0, buffer.GetLineIndexFromPosition(0));
        Assert.Equal(0, buffer.GetLineIndexFromPosition(4));
        Assert.Equal(1, buffer.GetLineIndexFromPosition(6));
        Assert.Equal(1, buffer.GetLineIndexFromPosition(10));
    }

    [Fact]
    public void TotalHeight_ShouldBeUpdatedOnLineHeightChange()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello\nWorld");
        buffer.LineHeight = 20;

        Assert.Equal(46, buffer.TotalHeight); // 2 lines * 20 + 6 padding
    }

    [Fact]
    public void LongestLineLength_ShouldBeUpdatedCorrectly()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello\nWorld\nLongerLine");

        Assert.Equal(10, buffer.LongestLineLength); // "LongerLine" is the longest line, without the newline
    }

    [Fact]
    public void IsLineSelected_ShouldReturnCorrectly()
    {
        var buffer = new TextBuffer();
        buffer.SetText("Hello\nWorld");

        Assert.True(buffer.IsLineSelected(0, 0, 5));
        Assert.False(buffer.IsLineSelected(0, 6, 10));
        Assert.True(buffer.IsLineSelected(1, 6, 10));
    }
}