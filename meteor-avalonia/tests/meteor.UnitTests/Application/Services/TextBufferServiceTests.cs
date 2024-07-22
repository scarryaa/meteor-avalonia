using System.Text;
using meteor.Core.Interfaces.Services;
using meteor.Core.Services;
using Moq;

namespace meteor.UnitTests.Application.Services;

public class TextBufferServiceTests
{
    [Fact]
    public void Constructor_WithInitialText_SetsLengthAndText()
    {
        var service = new TextBufferService("hello");
        Assert.Equal(5, service.Length);
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("hello", sb.ToString());
    }

    [Fact]
    public void Indexer_WithValidIndex_ReturnsCharacter()
    {
        var service = new TextBufferService("hello");
        Assert.Equal('e', service[1]);
    }

    [Fact]
    public void Indexer_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        var service = new TextBufferService("hello");
        Assert.Throws<ArgumentOutOfRangeException>(() => service[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => service[5]);
    }

    [Fact]
    public void Insert_WithValidIndex_InsertsText()
    {
        var service = new TextBufferService("hello");
        service.Insert(2, "world");
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("heworldllo", sb.ToString());
    }

    [Fact]
    public void Insert_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        var service = new TextBufferService("hello");
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Insert(-1, "world"));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Insert(6, "world"));
    }

    [Fact]
    public void Delete_WithValidIndexAndLength_DeletesText()
    {
        var service = new TextBufferService("hello world");
        service.Delete(6, 5);
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("hello ", sb.ToString());
    }

    [Fact]
    public void Delete_WithInvalidIndexOrLength_ThrowsArgumentOutOfRangeException()
    {
        var service = new TextBufferService("hello world");
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Delete(-1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Delete(6, 6));
    }

    [Fact]
    public void Substring_WithValidStartAndLength_ReturnsSubstring()
    {
        var service = new TextBufferService("hello world");
        Assert.Equal("world", service.Substring(6, 5));
    }

    [Fact]
    public void Substring_WithInvalidStartOrLength_ThrowsArgumentOutOfRangeException()
    {
        var service = new TextBufferService("hello world");
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Substring(-1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Substring(6, 6));
    }

    [Fact]
    public void ReplaceAll_ReplacesText()
    {
        var service = new TextBufferService("hello world");
        service.ReplaceAll("goodbye world");
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("goodbye world", sb.ToString());
    }

    [Fact]
    public void Iterate_IteratesOverText()
    {
        var service = new TextBufferService("hello");
        var result = "";
        service.Iterate(c => result += c);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Constructor_WithNullInitialText_SetsEmptyText()
    {
        var service = new TextBufferService();
        Assert.Equal(0, service.Length);
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("", sb.ToString());
    }

    [Fact]
    public void Insert_WithEmptyString_DoesNotChangeText()
    {
        var service = new TextBufferService("hello");
        service.Insert(2, "");
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("hello", sb.ToString());
    }

    [Fact]
    public void Delete_WithZeroLength_DoesNotChangeText()
    {
        var service = new TextBufferService("hello");
        service.Delete(2, 0);
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("hello", sb.ToString());
    }

    [Fact]
    public void Substring_WithZeroLength_ReturnsEmptyString()
    {
        var service = new TextBufferService("hello");
        Assert.Equal("", service.Substring(2, 0));
    }

    [Fact]
    public void ReplaceAll_WithEmptyString_SetsEmptyText()
    {
        var service = new TextBufferService("hello");
        service.ReplaceAll("");
        Assert.Equal(0, service.Length);
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("", sb.ToString());
    }

    [Fact]
    public void Iterate_WithEmptyText_DoesNotCallAction()
    {
        var service = new TextBufferService();
        var called = false;
        service.Iterate(c => called = true);
        Assert.False(called);
    }

    [Fact]
    public void AppendTo_AppendsAllText()
    {
        var service = new TextBufferService("hello world");
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("hello world", sb.ToString());
    }

    [Fact]
    public void AsSpan_ReturnsCorrectSpan()
    {
        var service = new TextBufferService("hello world");
        var span = service.AsSpan(6, 5);
        Assert.Equal("world", new string(span));
    }

    [Fact]
    public void CalculatePositionFromIndex_WithValidIndex_ReturnsCorrectPosition()
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(m => m.GetCharWidth()).Returns(7);
        textMeasurer.Setup(m => m.GetLineHeight()).Returns(10);

        var service = new TextBufferService("line1\nline2\nline3");
        var position = service.CalculatePositionFromIndex(8, textMeasurer.Object);

        var expectedX = 7 * 2;
        var expectedY = 10 * 1;

        Assert.Equal((expectedX, expectedY), position);
    }

    [Fact]
    public void CalculatePositionFromIndex_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(m => m.GetCharWidth()).Returns(7);
        textMeasurer.Setup(m => m.GetLineHeight()).Returns(10);

        var service = new TextBufferService("line1\nline2\nline3");
        Assert.Throws<ArgumentOutOfRangeException>(() => service.CalculatePositionFromIndex(-1, textMeasurer.Object));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.CalculatePositionFromIndex(100, textMeasurer.Object));
    }

    [Fact]
    public void GetLineNumberFromPosition_WithValidIndex_ReturnsCorrectLineNumber()
    {
        var service = new TextBufferService("line1\nline2\nline3");
        Assert.Equal(1, service.GetLineNumberFromPosition(0));
        Assert.Equal(2, service.GetLineNumberFromPosition(6));
        Assert.Equal(3, service.GetLineNumberFromPosition(12));
    }

    [Fact]
    public void GetLineNumberFromPosition_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        var service = new TextBufferService("line1\nline2\nline3");
        Assert.Throws<ArgumentOutOfRangeException>(() => service.GetLineNumberFromPosition(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.GetLineNumberFromPosition(100));
    }

    [Fact]
    public void GetLineText_WithValidLineNumber_ReturnsCorrectLineText()
    {
        var service = new TextBufferService("line1\nline2\nline3");
        Assert.Equal("line1", service.GetLineText(1));
        Assert.Equal("line2", service.GetLineText(2));
        Assert.Equal("line3", service.GetLineText(3));
    }

    [Fact]
    public void GetLineText_WithInvalidLineNumber_ThrowsArgumentOutOfRangeException()
    {
        var service = new TextBufferService("line1\nline2\nline3");
        Assert.Throws<ArgumentOutOfRangeException>(() => service.GetLineText(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.GetLineText(4));
    }

    [Fact]
    public void UpdateLineIndices_UpdatesLineIndicesCorrectly()
    {
        var service = new TextBufferService("line1\nline2");
        service.Insert(11, "\nline3");
        var lineCount = service.GetLineCount();
        Assert.Equal(3, lineCount);
        Assert.Equal("line3", service.GetLineText(3));
    }

    [Fact]
    public void PerformanceTest_LargeBuffer()
    {
        var text = new string('a', 10000); // Large buffer
        var service = new TextBufferService(text);
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal(text, sb.ToString());
    }

    [Fact]
    public void Insert_AtStart_UpdatesText()
    {
        var service = new TextBufferService("world");
        service.Insert(0, "hello ");
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("hello world", sb.ToString());
    }

    [Fact]
    public void Delete_AtStart_UpdatesText()
    {
        var service = new TextBufferService("hello world");
        service.Delete(0, 6); // Delete "hello "
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("world", sb.ToString());
    }

    [Fact]
    public void Insert_OverlapsEnd_UpdatesText()
    {
        var service = new TextBufferService("hello");
        service.Insert(5, " world");
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("hello world", sb.ToString());
    }

    [Fact]
    public void ReplaceAll_WithNull_SetsEmptyText()
    {
        var service = new TextBufferService("hello world");
        service.ReplaceAll(null);
        Assert.Equal(0, service.Length);
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Equal("", sb.ToString());
    }

    [Fact]
    public void AsSpan_WithInvalidArguments_ThrowsArgumentOutOfRangeException()
    {
        var service = new TextBufferService("hello world");
        Assert.Throws<ArgumentOutOfRangeException>(() => service.AsSpan(-1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.AsSpan(6, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.AsSpan(6, 100));
    }

    [Fact]
    public void CalculatePositionFromIndex_WithEdgeCases_ReturnsCorrectPosition()
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(m => m.GetCharWidth()).Returns(7);
        textMeasurer.Setup(m => m.GetLineHeight()).Returns(10);

        var service = new TextBufferService("line1\nline2\nline3");

        // Edge case at the start of each line
        Assert.Equal((0, 0), service.CalculatePositionFromIndex(0, textMeasurer.Object));
        Assert.Equal((0, 10), service.CalculatePositionFromIndex(6, textMeasurer.Object));
        Assert.Equal((0, 20), service.CalculatePositionFromIndex(12, textMeasurer.Object));

        // Edge case at the end of each line
        Assert.Equal((35, 0), service.CalculatePositionFromIndex(5, textMeasurer.Object));
        Assert.Equal((35, 10), service.CalculatePositionFromIndex(11, textMeasurer.Object));
        Assert.Equal((35, 20), service.CalculatePositionFromIndex(17, textMeasurer.Object));
    }

    [Fact]
    public void ConcurrentModifications_WorkCorrectly()
    {
        var service = new TextBufferService("hello world");

        // Use Task.Run to simulate concurrent modifications
        var tasks = new[]
        {
            Task.Run(() => service.Insert(5, " concurrent")),
            Task.Run(() => service.Delete(6, 5))
        };

        Task.WaitAll(tasks);

        var sb = new StringBuilder();
        service.AppendTo(sb);
        var result = sb.ToString();

        // Verify that "concurrent" is present in the result
        Assert.Contains("concurrent", result);
    }

    [Fact]
    public void PerformanceTest_LargeInsertion()
    {
        var text = new string('a', 10000);
        var service = new TextBufferService(text);
        service.Insert(5000, "insertedText");
        var sb = new StringBuilder();
        service.AppendTo(sb);
        Assert.Contains("insertedText", sb.ToString());
    }
}