using System.Text;
using meteor.Core.Enums.SyntaxHighlighting;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.Text;
using meteor.Core.Services;
using Moq;
using Xunit.Abstractions;

namespace meteor.UnitTests.Application.Services;

public class SyntaxHighlighterTests
{
    private readonly SyntaxHighlighter _highlighter;
    private readonly Mock<ITabService> _mockTabService;
    private readonly Mock<ITextBufferService> _mockTextBufferService;
    private readonly ITestOutputHelper _output;

    public SyntaxHighlighterTests(ITestOutputHelper output)
    {
        _output = output;
        _mockTabService = new Mock<ITabService>();
        _mockTextBufferService = new Mock<ITextBufferService>();
        _mockTabService.Setup(ts => ts.GetActiveTextBufferService()).Returns(_mockTextBufferService.Object);
        _highlighter = new SyntaxHighlighter(_mockTabService.Object);
    }

    [Fact]
    public void Highlight_EmptyText_ReturnsNoResults()
    {
        SetupTextBuffer("");

        var results = _highlighter.Highlight("");

        Assert.Empty(results);
    }

    [Fact]
    public void Highlight_SimpleKeyword_ReturnsCorrectResult()
    {
        var text = "if (condition)";
        SetupTextBuffer(text);

        var results = _highlighter.Highlight(text).ToList();

        Assert.Single(results);
        Assert.Equal(0, results[0].StartIndex);
        Assert.Equal(2, results[0].Length);
        Assert.Equal(SyntaxHighlightingType.Keyword, results[0].Type);
    }

    [Fact]
    public void Highlight_MultipleKeywords_ReturnsCorrectResults()
    {
        var text = "if (condition) { return true; } else { return false; }";
        SetupTextBuffer(text);

        var results = _highlighter.Highlight(text).ToList();

        Assert.Equal(4, results.Count);
        Assert.All(results, r => Assert.Equal(SyntaxHighlightingType.Keyword, r.Type));
    }

    [Fact]
    public void Highlight_SingleLineComment_ReturnsCorrectResult()
    {
        var text = "int x = 5; // This is a comment";
        SetupTextBuffer(text);

        var results = _highlighter.Highlight(text).ToList();

        Assert.Single(results);
        Assert.Equal(11, results[0].StartIndex);
        Assert.Equal(20, results[0].Length);
        Assert.Equal(SyntaxHighlightingType.Comment, results[0].Type);
    }

    [Fact]
    public void Highlight_MultiLineComment_ReturnsCorrectResult()
    {
        var text = "int x = 5;\n/* This is a\nmulti-line comment */\nint y = 10;";
        SetupTextBuffer(text);

        var results = _highlighter.Highlight(text).ToList();

        Assert.Single(results);
        Assert.Equal(11, results[0].StartIndex);
        Assert.Equal(34, results[0].Length);
        Assert.Equal(SyntaxHighlightingType.Comment, results[0].Type);
    }

    [Fact]
    public void Highlight_String_ReturnsCorrectResult()
    {
        var text = "string message = \"Hello, World!\";";
        SetupTextBuffer(text);

        var results = _highlighter.Highlight(text).ToList();

        Assert.Single(results);
        Assert.Equal(17, results[0].StartIndex);
        Assert.Equal(15, results[0].Length);
        Assert.Equal(SyntaxHighlightingType.String, results[0].Type);
    }

    [Fact]
    public void Highlight_MixedContent_ReturnsCorrectResults()
    {
        var text = "if (x == 5) { // Check condition\n    Console.WriteLine(\"x is 5\"); // Print result\n}";
        SetupTextBuffer(text);

        var results = _highlighter.Highlight(text).ToList();

        // Print results for debugging
        foreach (var result in results)
            _output.WriteLine($"Start: {result.StartIndex}, Length: {result.Length}, Type: {result.Type}");

        Assert.Equal(4, results.Count);
        Assert.Equal(SyntaxHighlightingType.Keyword, results[0].Type);
        Assert.Equal(SyntaxHighlightingType.Comment, results[1].Type);
        Assert.Equal(SyntaxHighlightingType.Comment, results[2].Type);
        Assert.Equal(SyntaxHighlightingType.String, results[3].Type);
    }

    [Fact]
    public void Highlight_WithTextChange_UpdatesHighlightingCorrectly()
    {
        var initialText = "if (x == 5) { Console.WriteLine(\"x is 5\"); }";
        SetupTextBuffer(initialText);

        // Initial highlight
        var initialResults = _highlighter.Highlight(initialText).ToList();

        // Check initial highlighting results
        Assert.Equal(2, initialResults.Count);
        Assert.Equal(SyntaxHighlightingType.Keyword, initialResults[0].Type);
        Assert.Equal(SyntaxHighlightingType.String, initialResults[1].Type);

        // Log initial results
        _output.WriteLine("Initial Results:");
        foreach (var result in initialResults)
            _output.WriteLine($"Type: {result.Type}, StartIndex: {result.StartIndex}, Length: {result.Length}");

        // Change text
        var changeInfo = new TextChangeInfo
        {
            StartPosition = 4,
            EndPosition = 9,
            NewText = "y == 10"
        };

        var updatedText = "if (y == 10) { Console.WriteLine(\"x is 5\"); }";
        SetupTextBuffer(updatedText);

        // Highlight updated text
        var results = _highlighter.Highlight(updatedText, changeInfo).ToList();

        // Log updated results
        _output.WriteLine("Updated Results:");
        foreach (var result in results)
            _output.WriteLine($"Type: {result.Type}, StartIndex: {result.StartIndex}, Length: {result.Length}");

        // Assert results
        Assert.Equal(2, results.Count);
        Assert.Equal(SyntaxHighlightingType.Keyword, results[0].Type);
        Assert.Equal(SyntaxHighlightingType.String, results[1].Type);

        // Additional checks for updated text
        Assert.Equal(0, results[0].StartIndex); // "if" keyword
        Assert.Equal(2, results[0].Length);

        var stringResult = results.First(r => r.Type == SyntaxHighlightingType.String);
        Assert.Equal(updatedText.IndexOf("\"x is 5\""), stringResult.StartIndex);
    }

    [Fact]
    public void Highlight_SmallText_ProcessesCorrectly()
    {
        var smallText = "if (x == 0) { Console.WriteLine(\"x is 0\"); }";
        SetupTextBuffer(smallText);

        var results = _highlighter.Highlight(smallText).ToList();

        _output.WriteLine($"Total results: {results.Count}");
        _output.WriteLine($"Keywords: {results.Count(r => r.Type == SyntaxHighlightingType.Keyword)}");
        _output.WriteLine($"Strings: {results.Count(r => r.Type == SyntaxHighlightingType.String)}");

        foreach (var result in results.Where(r => r.Type == SyntaxHighlightingType.String))
            _output.WriteLine($"String: StartIndex={result.StartIndex}, Length={result.Length}");

        Assert.True(results.Count >= 2, $"Expected at least 2 results, but got {results.Count}");
        Assert.Equal(1, results.Count(r => r.Type == SyntaxHighlightingType.Keyword));
        Assert.Equal(1, results.Count(r => r.Type == SyntaxHighlightingType.String));

        // Check the first few results to ensure they're correct
        Assert.Equal(SyntaxHighlightingType.Keyword, results[0].Type);
        Assert.Equal(0, results[0].StartIndex);
        Assert.Equal(2, results[0].Length);

        Assert.Equal(smallText.IndexOf("\"x is 0\""), results[1].StartIndex);
        Assert.Equal("\"x is 0\"".Length, results[1].Length);
    }

    [Fact]
    public void TestHighlighting()
    {
        var fullText = "var x = 0; // Example\n\"x is 0\"";
        var expectedStartIndex = fullText.IndexOf("\"x is 0\"");
        var syntaxHighlighter = new SyntaxHighlighter(_mockTabService.Object);
        var results = syntaxHighlighter.Highlight(fullText).ToList();

        // Debug output
        _output.WriteLine($"Total results: {results.Count}");
        _output.WriteLine($"Keywords: {results.Count(r => r.Type == SyntaxHighlightingType.Keyword)}");
        _output.WriteLine($"Strings: {results.Count(r => r.Type == SyntaxHighlightingType.String)}");

        foreach (var result in results.Where(r => r.Type == SyntaxHighlightingType.String))
            _output.WriteLine($"String: StartIndex={result.StartIndex}, Length={result.Length}");

        Assert.Equal(expectedStartIndex, results[2].StartIndex);
    }

    [Fact]
    public void Highlight_LargeText_ProcessesInChunks()
    {
        var largeText = new StringBuilder();
        for (var i = 0; i < 1000; i++) largeText.AppendLine($"if (x == {i}) {{ Console.WriteLine(\"x is {i}\"); }}");
        var fullText = largeText.ToString();
        SetupTextBuffer(fullText);

        var results = _highlighter.Highlight(fullText).ToList();

        _output.WriteLine($"Total results: {results.Count}");
        _output.WriteLine($"Keywords: {results.Count(r => r.Type == SyntaxHighlightingType.Keyword)}");
        _output.WriteLine($"Strings: {results.Count(r => r.Type == SyntaxHighlightingType.String)}");

        foreach (var result in results.Where(r => r.Type == SyntaxHighlightingType.String))
            _output.WriteLine($"String: StartIndex={result.StartIndex}, Length={result.Length}");

        Assert.True(results.Count >= 2000, $"Expected at least 2000 results, but got {results.Count}");
        Assert.Equal(1000, results.Count(r => r.Type == SyntaxHighlightingType.Keyword));
        Assert.Equal(1000, results.Count(r => r.Type == SyntaxHighlightingType.String));

        // Check the first few results to ensure they're correct
        Assert.Equal(SyntaxHighlightingType.Keyword, results[0].Type);
        Assert.Equal(0, results[0].StartIndex);
        Assert.Equal(2, results[0].Length);
    }

    private void SetupTextBuffer(string text)
    {
        _mockTextBufferService.Setup(tbs => tbs.Length).Returns(text.Length);
        _mockTextBufferService.Setup(tbs => tbs.Substring(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int start, int length) => text.Substring(start, length));
        _mockTextBufferService.Setup(tbs => tbs.AppendTo(It.IsAny<StringBuilder>()))
            .Callback<StringBuilder>(sb => sb.Append(text));
    }
}