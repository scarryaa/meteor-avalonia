using meteor.Core.Models;
using Xunit.Abstractions;

namespace meteor.Core.Tests;

public class RopeTests
{
    private readonly ITestOutputHelper _output;

    public RopeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ShouldInitializeRopeCorrectly()
    {
        var rope = new Rope("Hello, World!");
        Assert.Equal(13, rope.Length);
        Assert.Equal(1, rope.LineCount);
    }

    [Fact]
    public void GetText_ShouldReturnEntireText()
    {
        var rope = new Rope("Hello, World!");
        Assert.Equal("Hello, World!", rope.GetText());
    }

    [Theory]
    [InlineData(0, 5, "Hello")]
    [InlineData(7, 5, "World")]
    [InlineData(0, 13, "Hello, World!")]
    public void GetText_WithStartAndLength_ShouldReturnCorrectSubstring(int start, int length, string expected)
    {
        var rope = new Rope("Hello, World!");
        Assert.Equal(expected, rope.GetText(start, length));
    }

    [Fact]
    public void Insert_ShouldInsertTextCorrectly()
    {
        var rope = new Rope("Hello, World!");
        rope.Insert(7, "Beautiful ");
        Assert.Equal("Hello, Beautiful World!", rope.GetText());
    }

    [Fact]
    public void Insert_AtBeginning_ShouldInsertCorrectly()
    {
        var rope = new Rope("World!");
        rope.Insert(0, "Hello, ");
        Assert.Equal("Hello, World!", rope.GetText());
    }

    [Fact]
    public void Insert_AtEnd_ShouldInsertCorrectly()
    {
        var rope = new Rope("Hello");
        rope.Insert(5, ", World!");
        Assert.Equal("Hello, World!", rope.GetText());
    }

    [Fact]
    public void Insert_BeyondLength_ShouldInsertAtEnd()
    {
        var rope = new Rope("Hello");
        rope.Insert(10, ", World!");
        Assert.Equal("Hello, World!", rope.GetText());
    }

    [Fact]
    public void Delete_ShouldDeleteTextCorrectly()
    {
        var rope = new Rope("Hello, Beautiful World!");
        rope.Delete(7, 10);
        Assert.Equal("Hello, World!", rope.GetText());
    }

    [Fact]
    public void Delete_FromBeginning_ShouldDeleteCorrectly()
    {
        var rope = new Rope("Hello, World!");
        rope.Delete(0, 7);
        Assert.Equal("World!", rope.GetText());
    }

    [Fact]
    public void Delete_ToEnd_ShouldDeleteCorrectly()
    {
        var rope = new Rope("Hello, World!");
        rope.Delete(5, 8);
        Assert.Equal("Hello", rope.GetText());
    }

    [Fact]
    public void Delete_EntireString_ShouldResultInEmptyRope()
    {
        var rope = new Rope("Hello, World!");
        rope.Delete(0, 13);
        Assert.Equal(0, rope.Length);
        Assert.Equal("", rope.GetText());
    }

    [Fact]
    public void Delete_BeyondLength_ShouldDeleteToEnd()
    {
        var rope = new Rope("Hello, World!");
        rope.Delete(7, 100);
        Assert.Equal("Hello, ", rope.GetText());
    }

    [Fact]
    public void LineCount_ShouldReturnCorrectNumberOfLines()
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou");
        Assert.Equal(5, rope.LineCount);
    }

    [Theory]
    [InlineData(0, "Hello\n")]
    [InlineData(1, "World\n")]
    [InlineData(4, "You")]
    public void GetLineText_ShouldReturnCorrectLine(int lineIndex, string expected)
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou");
        Assert.Equal(expected, rope.GetLineText(lineIndex));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 0)]
    [InlineData(6, 1)]
    [InlineData(20, 4)]
    public void GetLineIndexFromPosition_ShouldReturnCorrectLineIndex(int position, int expectedLineIndex)
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou");
        Assert.Equal(expectedLineIndex, rope.GetLineIndexFromPosition(position));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 6)]
    [InlineData(4, 20)]
    public void GetLineStartPosition_ShouldReturnCorrectStartPosition(int lineIndex, int expectedStartPosition)
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou");
        Assert.Equal(expectedStartPosition, rope.GetLineStartPosition(lineIndex));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 11)]
    [InlineData(4, 23)]
    public void GetLineEndPosition_ShouldReturnCorrectEndPosition(int lineIndex, int expectedEndPosition)
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou");
        Assert.Equal(expectedEndPosition, rope.GetLineEndPosition(lineIndex));
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(1, 6)]
    [InlineData(4, 3)]
    public void GetLineLength_ShouldReturnCorrectLength(int lineIndex, int expectedLength)
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou");
        Assert.Equal(expectedLength, rope.GetLineLength(lineIndex));
    }

    [Theory]
    [InlineData('o', 4)]
    [InlineData('W', 6)]
    [InlineData('z', -1)]
    public void IndexOf_ShouldReturnCorrectIndex(char value, int expectedIndex)
    {
        var rope = new Rope("Hello\nWorld");
        Assert.Equal(expectedIndex, rope.IndexOf(value));
    }

    [Theory]
    [InlineData('o', 5, 7)]
    [InlineData('W', 6, 6)]
    [InlineData('z', 0, -1)]
    public void IndexOf_WithStartIndex_ShouldReturnCorrectIndex(char value, int startIndex, int expectedIndex)
    {
        var rope = new Rope("Hello\nWorld");
        Assert.Equal(expectedIndex, rope.IndexOf(value, startIndex));
    }

    [Fact]
    public void LargeRope_ShouldHandleOperationsEfficiently()
    {
        var largeText = new string('A', 10000) + new string('B', 10000) + new string('C', 10000);
        var rope = new Rope(largeText);

        _output.WriteLine($"Initial rope length: {rope.Length}");
        Assert.Equal(30000, rope.Length);
        Assert.Equal('B', rope.GetText(10000, 1)[0]);

        rope.Insert(15000, "XYZ");
        _output.WriteLine($"After insertion, rope length: {rope.Length}");
        Assert.Equal("BXYZB", rope.GetText(14999, 5));

        rope.Delete(10000, 5000);
        _output.WriteLine($"After deletion, rope length: {rope.Length}");
        _output.WriteLine($"Text at 9995-10005: {rope.GetText(9995, 10)}");
        Assert.Equal(25003, rope.Length);
        Assert.Equal('X', rope.GetText(10000, 1)[0]);

        // Add more aggressive deletion test
        rope.Delete(0, 20000);
        _output.WriteLine($"After large deletion, rope length: {rope.Length}");
        _output.WriteLine($"First 10 characters: {rope.GetText(0, 10)}");
        Assert.Equal(5003, rope.Length);
        Assert.Equal('C', rope.GetText(0, 1)[0]);

        // Test deleting beyond the rope's length
        rope.Delete(5000, 10000);
        _output.WriteLine($"After deletion beyond length, rope length: {rope.Length}");
        Assert.Equal(5000, rope.Length);
    }
}