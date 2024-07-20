using System.Collections.Concurrent;
using System.Diagnostics;
using meteor.Core.Entities;
using Xunit.Abstractions;

namespace meteor.UnitTests.Core.Entities;

public class TextBufferTests
{
    private readonly ITestOutputHelper _output;

    public TextBufferTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_WithInitialText_SetsLengthAndText()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            Assert.Equal(5, textBuffer.Length);
            Assert.Equal("hello", textBuffer.GetText());
        }
    }

    [Fact]
    public void Indexer_WithValidIndex_ReturnsCharacter()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            Assert.Equal('e', textBuffer[1]);
        }
    }

    [Fact]
    public void Indexer_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            Assert.Throws<IndexOutOfRangeException>(() => textBuffer[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => textBuffer[5]);
        }
    }

    [Fact]
    public void Insert_WithValidIndex_InsertsText()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            textBuffer.Insert(2, "world");
            Assert.Equal("heworldllo", textBuffer.GetText());
        }
    }

    [Fact]
    public void Insert_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.Insert(-1, "world"));
            Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.Insert(6, "world"));
        }
    }

    [Fact]
    public void Delete_WithValidIndexAndLength_DeletesText()
    {
        using (var textBuffer = new TextBuffer("hello world"))
        {
            textBuffer.Delete(6, 5);
            Assert.Equal("hello ", textBuffer.GetText());
        }
    }

    [Fact]
    public void Delete_WithInvalidIndexOrLength_ThrowsArgumentOutOfRangeException()
    {
        using (var textBuffer = new TextBuffer("hello world"))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.Delete(-1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.Delete(6, 6));
        }
    }

    [Fact]
    public void Substring_WithValidStartAndLength_ReturnsSubstring()
    {
        using (var textBuffer = new TextBuffer("hello world"))
        {
            Assert.Equal("world", textBuffer.Substring(6, 5));
        }
    }

    [Fact]
    public void Substring_WithInvalidStartOrLength_ThrowsArgumentOutOfRangeException()
    {
        using (var textBuffer = new TextBuffer("hello world"))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.Substring(-1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.Substring(6, 6));
        }
    }

    [Fact]
    public void GetText_ReturnsText()
    {
        using (var textBuffer = new TextBuffer("hello world"))
        {
            Assert.Equal("hello world", textBuffer.GetText());
        }
    }

    [Fact]
    public void ReplaceAll_ReplacesText()
    {
        using (var textBuffer = new TextBuffer("hello world"))
        {
            textBuffer.ReplaceAll("goodbye world");
            Assert.Equal("goodbye world", textBuffer.GetText());
        }
    }

    [Fact]
    public void Iterate_IteratesOverText()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            var result = "";
            textBuffer.Iterate(c => result += c);
            Assert.Equal("hello", result);
        }
    }

    [Fact]
    public void Constructor_WithNullInitialText_SetsEmptyText()
    {
        using (var textBuffer = new TextBuffer())
        {
            Assert.Equal(0, textBuffer.Length);
            Assert.Equal("", textBuffer.GetText());
        }
    }

    [Fact]
    public void Insert_WithEmptyString_DoesNotChangeText()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            textBuffer.Insert(2, "");
            Assert.Equal("hello", textBuffer.GetText());
        }
    }

    [Fact]
    public void Delete_WithZeroLength_DoesNotChangeText()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            textBuffer.Delete(2, 0);
            Assert.Equal("hello", textBuffer.GetText());
        }
    }

    [Fact]
    public void Substring_WithZeroLength_ReturnsEmptyString()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            Assert.Equal("", textBuffer.Substring(2, 0));
        }
    }

    [Fact]
    public void ReplaceAll_WithEmptyString_SetsEmptyText()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            textBuffer.ReplaceAll("");
            Assert.Equal(0, textBuffer.Length);
            Assert.Equal("", textBuffer.GetText());
        }
    }

    [Fact]
    public void Iterate_WithEmptyText_DoesNotCallAction()
    {
        using (var textBuffer = new TextBuffer())
        {
            var called = false;
            textBuffer.Iterate(c => called = true);
            Assert.False(called);
        }
    }

    [Fact]
    public void ConcurrentAccess_IsThreadSafe()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            var totalInsertions = 100000;
            var batchSize = 1000;

            var insertedLengths = new ConcurrentQueue<int>();

            Parallel.For(0, totalInsertions / batchSize, _ =>
            {
                for (var i = 0; i < batchSize; i++)
                {
                    var text = "x";
                    textBuffer.Insert(i % (textBuffer.Length + 1), text);
                    insertedLengths.Enqueue(text.Length);
                }
            });

            // Ensure all insertions are processed
            textBuffer.EnsureAllInsertionsProcessed();

            var expectedLength = 5 + insertedLengths.Sum();
            var actualLength = textBuffer.Length;

            _output.WriteLine($"Expected Length: {expectedLength}");
            _output.WriteLine($"Actual Length: {actualLength}");

            Assert.Equal(expectedLength, actualLength);
            Assert.Equal(expectedLength, textBuffer.GetText().Length);
        }
    }

    [Fact]
    public void ConcurrentAccess_PerformanceTest()
    {
        using (var textBuffer = new TextBuffer("hello"))
        {
            var totalInsertions = 100000;
            var batchSize = 1000;

            var stopwatch = Stopwatch.StartNew();

            Parallel.For(0, totalInsertions / batchSize, _ =>
            {
                for (var i = 0; i < batchSize; i++) textBuffer.Insert(i % (textBuffer.Length + 1), "x");
            });

            // Ensure all insertions are processed
            textBuffer.EnsureAllInsertionsProcessed();

            var expectedLength = 100005;
            var actualLength = textBuffer.Length;

            _output.WriteLine($"Expected Length: {expectedLength}");
            _output.WriteLine($"Actual Length: {actualLength}");

            stopwatch.Stop();
            _output.WriteLine($"Total Time: {stopwatch.ElapsedMilliseconds} ms");

            Assert.Equal(expectedLength, actualLength);
            Assert.Equal(expectedLength, textBuffer.GetText().Length);
        }
    }
}