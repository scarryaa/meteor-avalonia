using meteor.Core.Interfaces;
using meteor.Core.Models;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace meteor.Core.Tests;

public class RopeTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger _logger;
    
    public RopeTests(ITestOutputHelper output)
    {
        _output = output;
        var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
        _logger = loggerFactory.CreateLogger<IRope>();
    }

    [Fact]
    public void Constructor_ShouldInitializeRopeCorrectly()
    {
        var rope = new Rope("Hello, World!", _logger);
        Assert.Equal(13, rope.Length);
        Assert.Equal(1, rope.LineCount);
    }

    [Fact]
    public void GetText_ShouldReturnEntireText()
    {
        var rope = new Rope("Hello, World!", _logger);
        Assert.Equal("Hello, World!", rope.GetText());
    }

    [Theory]
    [InlineData(0, 5, "Hello")]
    [InlineData(7, 5, "World")]
    [InlineData(0, 13, "Hello, World!")]
    public void GetText_WithStartAndLength_ShouldReturnCorrectSubstring(int start, int length, string expected)
    {
        var rope = new Rope("Hello, World!", _logger);
        Assert.Equal(expected, rope.GetText(start, length));
    }

    [Fact]
    public void Insert_ShouldInsertTextCorrectly()
    {
        var rope = new Rope("Hello, World!", _logger);
        rope.Insert(7, "Beautiful ");
        Assert.Equal("Hello, Beautiful World!", rope.GetText());
    }

    [Fact]
    public void Insert_AtBeginning_ShouldInsertCorrectly()
    {
        var rope = new Rope("World!", _logger);
        rope.Insert(0, "Hello, ");
        Assert.Equal("Hello, World!", rope.GetText());
    }

    [Fact]
    public void Insert_AtEnd_ShouldInsertCorrectly()
    {
        var rope = new Rope("Hello", _logger);
        rope.Insert(5, ", World!");
        Assert.Equal("Hello, World!", rope.GetText());
    }

    [Fact]
    public void Insert_BeyondLength_ShouldInsertAtEnd()
    {
        var rope = new Rope("Hello", _logger);
        rope.Insert(10, ", World!");
        Assert.Equal("Hello, World!", rope.GetText());
    }

    [Fact]
    public void Delete_ShouldDeleteTextCorrectly()
    {
        var rope = new Rope("Hello, Beautiful World!", _logger);
        rope.Delete(7, 10);
        Assert.Equal("Hello, World!", rope.GetText());
    }

    [Fact]
    public void Delete_FromBeginning_ShouldDeleteCorrectly()
    {
        var rope = new Rope("Hello, World!", _logger);
        rope.Delete(0, 7);
        Assert.Equal("World!", rope.GetText());
    }

    [Fact]
    public void Delete_ToEnd_ShouldDeleteCorrectly()
    {
        var rope = new Rope("Hello, World!", _logger);
        rope.Delete(5, 8);
        Assert.Equal("Hello", rope.GetText());
    }

    [Fact]
    public void Delete_EntireString_ShouldResultInEmptyRope()
    {
        var rope = new Rope("Hello, World!", _logger);
        rope.Delete(0, 13);
        Assert.Equal(0, rope.Length);
        Assert.Equal("", rope.GetText());
    }

    [Fact]
    public void Delete_BeyondLength_ShouldDeleteToEnd()
    {
        var rope = new Rope("Hello, World!", _logger);
        rope.Delete(7, 100);
        Assert.Equal("Hello, ", rope.GetText());
    }

    [Fact]
    public void LineCount_ShouldReturnCorrectNumberOfLines()
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou", _logger);
        Assert.Equal(5, rope.LineCount);
    }

    [Theory]
    [InlineData(0, "Hello")]
    [InlineData(1, "World")]
    [InlineData(4, "You")]
    public void GetLineText_ShouldReturnCorrectLine(int lineIndex, string expected)
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou", _logger);
        Assert.Equal(expected, rope.GetLineText(lineIndex));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 0)]
    [InlineData(6, 1)]
    [InlineData(20, 4)]
    public void GetLineIndexFromPosition_ShouldReturnCorrectLineIndex(int position, int expectedLineIndex)
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou", _logger);
        Assert.Equal(expectedLineIndex, rope.GetLineIndexFromPosition(position));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 6)]
    [InlineData(4, 20)]
    public void GetLineStartPosition_ShouldReturnCorrectStartPosition(int lineIndex, int expectedStartPosition)
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou", _logger);
        Assert.Equal(expectedStartPosition, rope.GetLineStartPosition(lineIndex));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 11)]
    [InlineData(4, 23)]
    public void GetLineEndPosition_ShouldReturnCorrectEndPosition(int lineIndex, int expectedEndPosition)
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou", _logger);
        Assert.Equal(expectedEndPosition, rope.GetLineEndPosition(lineIndex));
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(1, 6)]
    [InlineData(4, 3)]
    public void GetLineLength_ShouldReturnCorrectLength(int lineIndex, int expectedLength)
    {
        var rope = new Rope("Hello\nWorld\nHow\nAre\nYou", _logger);
        Assert.Equal(expectedLength, rope.GetLineLength(lineIndex));
    }

    [Theory]
    [InlineData('o', 0, 4)]
    [InlineData('W', 0, 6)]
    [InlineData('z', 0, -1)]
    public void IndexOf_ShouldReturnCorrectIndex(char value, int startIndex, int expectedIndex)
    {
        var rope = new Rope("Hello\nWorld", _logger);
        Assert.Equal(expectedIndex, rope.IndexOf(value, startIndex));
    }

    [Theory]
    [InlineData('o', 5, 7)]
    [InlineData('W', 6, 6)]
    [InlineData('z', 0, -1)]
    public void IndexOf_WithStartIndex_ShouldReturnCorrectIndex(char value, int startIndex, int expectedIndex)
    {
        var rope = new Rope("Hello\nWorld", _logger);
        Assert.Equal(expectedIndex, rope.IndexOf(value, startIndex));
    }

    [Fact]
    public void LargeRope_ShouldHandleOperationsEfficiently()
    {
        var largeText = new string('A', 10000) + new string('B', 10000) + new string('C', 10000);
        var rope = new Rope(largeText, _logger);

        _output.WriteLine($"Initial rope length: {rope.Length}");
        Assert.Equal(30000, rope.Length);

        _output.WriteLine("Checking character at index 10000");
        var charAt10000 = rope.GetText(10000, 1);
        _output.WriteLine($"Character at 10000: {charAt10000}");
        Assert.Equal("B", charAt10000);

        _output.WriteLine("Inserting 'XYZ' at index 15000");
        rope.Insert(15000, "XYZ");
        _output.WriteLine($"After insertion, rope length: {rope.Length}");

        _output.WriteLine("Checking text at indices 14999-15004");
        var textAt14999 = rope.GetText(14999, 5);
        _output.WriteLine($"Text at 14999-15004: {textAt14999}");
        Assert.Equal("BXYZB", textAt14999);

        _output.WriteLine("Deleting 5000 characters starting from index 10000");
        rope.Delete(10000, 5000);
        _output.WriteLine($"After deletion, rope length: {rope.Length}");

        _output.WriteLine("Checking text at indices 9995-10005");
        var textAt9995 = rope.GetText(9995, 10);
        _output.WriteLine($"Text at 9995-10005: {textAt9995}");
        Assert.Equal(25003, rope.Length);

        _output.WriteLine("Checking character at index 10000");
        var charAtIndex10000 = rope.GetText(10000, 1);
        _output.WriteLine($"Character at 10000: {charAtIndex10000}");
        Assert.Equal("X", charAtIndex10000);

        _output.WriteLine("Deleting 20000 characters starting from index 0");
        rope.Delete(0, 20000);
        _output.WriteLine($"After large deletion, rope length: {rope.Length}");

        _output.WriteLine("Checking first 10 characters");
        var firstTenChars = rope.GetText(0, Math.Min(10, rope.Length));
        _output.WriteLine($"First 10 characters: {firstTenChars}");
        Assert.Equal(5003, rope.Length);
        Assert.Equal("C", firstTenChars.Substring(0, 1));

        _output.WriteLine("Deleting 10000 characters starting from index 5000");
        rope.Delete(5000, 10000);
        _output.WriteLine($"After deletion beyond length, rope length: {rope.Length}");
        Assert.Equal(5000, rope.Length);
    }

    [Fact]
    public void Insert_MultipleTimes_ShouldUpdateCorrectly()
    {
        var rope = new Rope("Hello", _logger);
        rope.Insert(5, " World");
        rope.Insert(0, "Say: ");
        rope.Insert(10, "Beautiful ");
        Assert.Equal("Say: HelloBeautiful  World", rope.GetText());
    }

    [Fact]
    public void Delete_MultipleTimes_ShouldUpdateCorrectly()
    {
        var rope = new Rope("Hello Beautiful World", _logger);
        rope.Delete(5, 10);
        rope.Delete(0, 1);
        rope.Delete(rope.Length - 1, 1);
        Assert.Equal("ello Worl", rope.GetText());
    }

    [Fact]
    public void Insert_AndDelete_ShouldWorkTogether()
    {
        var rope = new Rope("Hello World", _logger);
        rope.Insert(6, "Beautiful ");
        rope.Delete(0, 6);
        Assert.Equal("Beautiful World", rope.GetText());
    }

    [Theory]
    [InlineData("Hello\nWorld\n", 3)]
    [InlineData("\n\n\n", 4)]
    [InlineData("No newlines", 1)]
    public void LineCount_VariousInputs_ShouldReturnCorrectCount(string input, int expectedLineCount)
    {
        var rope = new Rope(input, _logger);
        Assert.Equal(expectedLineCount, rope.LineCount);
    }

    [Fact]
    public void GetLineText_EmptyLines_ShouldReturnEmptyString()
    {
        var rope = new Rope("\n\n\n", _logger);
        Assert.Equal("", rope.GetLineText(1));
    }

    [Fact]
    public void GetLineIndexFromPosition_EndOfFile_ShouldReturnLastLineIndex()
    {
        var rope = new Rope("Hello\nWorld\n", _logger);
        Assert.Equal(2, rope.GetLineIndexFromPosition(rope.Length));
    }

    [Fact]
    public void Insert_NewlineCharacters_ShouldUpdateLineCount()
    {
        var rope = new Rope("Hello World", _logger);
        rope.Insert(5, "\n");
        Assert.Equal(2, rope.LineCount);
        rope.Insert(0, "\n\n");
        Assert.Equal(4, rope.LineCount);
    }
}