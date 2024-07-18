using meteor.Application.Services;

namespace meteor.UnitTests.Application.Services;

public class TextBufferServiceTests
{
    [Fact]
    public void Constructor_WithInitialText_SetsLengthAndText()
    {
        var service = new TextBufferService("hello");
        Assert.Equal(5, service.Length);
        Assert.Equal("hello", service.GetText());
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
        Assert.Equal("heworldllo", service.GetText());
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
        Assert.Equal("hello ", service.GetText());
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
    public void GetText_ReturnsText()
    {
        var service = new TextBufferService("hello world");
        Assert.Equal("hello world", service.GetText());
    }

    [Fact]
    public void ReplaceAll_ReplacesText()
    {
        var service = new TextBufferService("hello world");
        service.ReplaceAll("goodbye world");
        Assert.Equal("goodbye world", service.GetText());
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
        Assert.Equal("", service.GetText());
    }

    [Fact]
    public void Insert_WithEmptyString_DoesNotChangeText()
    {
        var service = new TextBufferService("hello");
        service.Insert(2, "");
        Assert.Equal("hello", service.GetText());
    }

    [Fact]
    public void Delete_WithZeroLength_DoesNotChangeText()
    {
        var service = new TextBufferService("hello");
        service.Delete(2, 0);
        Assert.Equal("hello", service.GetText());
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
        Assert.Equal("", service.GetText());
    }

    [Fact]
    public void Iterate_WithEmptyText_DoesNotCallAction()
    {
        var service = new TextBufferService();
        var called = false;
        service.Iterate(c => called = true);
        Assert.False(called);
    }
}