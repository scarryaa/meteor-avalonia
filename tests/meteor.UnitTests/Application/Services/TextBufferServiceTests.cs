using System.Text;
using meteor.Core.Services;

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
        Assert.Throws<IndexOutOfRangeException>(() => service[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => service[5]);
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
}