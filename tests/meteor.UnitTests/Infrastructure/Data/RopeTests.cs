using System.Diagnostics;
using meteor.Infrastructure.Data;
using Xunit.Abstractions;

namespace meteor.UnitTests.Infrastructure.Data;

public class RopeTests
{
    private readonly ITestOutputHelper _output;

    public RopeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_EmptyString_ReturnsEmptyRope()
    {
        var rope = new Rope();
        Assert.Equal(0, rope.Length);
    }

    [Fact]
    public void Constructor_NonEmptyString_ReturnsRopeWithCorrectLength()
    {
        var rope = new Rope("hello");
        Assert.Equal(5, rope.Length);
    }

    [Fact]
    public void Length_EmptyRope_ReturnsZero()
    {
        var rope = new Rope();
        Assert.Equal(0, rope.Length);
    }

    [Fact]
    public void Length_NonEmptyRope_ReturnsCorrectLength()
    {
        var rope = new Rope("hello");
        Assert.Equal(5, rope.Length);
    }

    [Fact]
    public void Indexer_ValidIndex_ReturnsCorrectCharacter()
    {
        var rope = new Rope("hello");
        Assert.Equal('e', rope[1]);
    }

    [Fact]
    public void Indexer_InvalidIndex_ThrowsIndexOutOfRangeException()
    {
        var rope = new Rope("hello");
        Assert.Throws<IndexOutOfRangeException>(() => rope[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => rope[5]);
    }

    [Fact]
    public void Insert_ValidIndex_ReturnsNewRopeWithInsertedString()
    {
        var rope = new Rope("hello");
        var newRope = rope.Insert(2, "world");
        Assert.Equal("heworldllo", newRope.ToString());
    }

    [Fact]
    public void Insert_InvalidIndex_ThrowsIndexOutOfRangeException()
    {
        var rope = new Rope("hello");
        Assert.Throws<IndexOutOfRangeException>(() => rope.Insert(-1, "world"));
        Assert.Throws<IndexOutOfRangeException>(() => rope.Insert(6, "world"));
    }

    [Fact]
    public void Delete_ValidIndexAndLength_ReturnsNewRopeWithDeletedSubstring()
    {
        var rope = new Rope("hello world");
        var newRope = rope.Delete(6, 5);
        Assert.Equal("hello ", newRope.ToString());
    }

    [Fact]
    public void Delete_InvalidIndexOrLength_ThrowsIndexOutOfRangeException()
    {
        var rope = new Rope("hello world");
        Assert.Throws<IndexOutOfRangeException>(() => rope.Delete(-1, 5));
        Assert.Throws<IndexOutOfRangeException>(() => rope.Delete(6, 6));
    }

    [Fact]
    public void Substring_ValidStartAndLength_ReturnsNewRopeWithSubstring()
    {
        var rope = new Rope("hello world");
        var newRope = rope.Substring(6, 5);
        Assert.Equal("world", newRope);
    }

    [Fact]
    public void Substring_InvalidStartOrLength_ThrowsArgumentOutOfRangeException()
    {
        var rope = new Rope("hello world");
        Assert.Throws<ArgumentOutOfRangeException>(() => rope.Substring(-1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => rope.Substring(40, 6));
    }

    [Fact]
    public void ToString_ReturnsStringRepresentationOfRope()
    {
        var rope = new Rope("hello world");
        Assert.Equal("hello world", rope.ToString());
    }

    [Fact]
    public void Concat_ValidRope_ReturnsNewRopeWithConcatenatedStrings()
    {
        var rope1 = new Rope("hello");
        var rope2 = new Rope(" world");
        var newRope = rope1.Concat(rope2);
        Assert.Equal("hello world", newRope.ToString());
    }

    [Fact]
    public void Concat_InvalidArgument_ThrowsArgumentException()
    {
        var rope = new Rope("hello");
        Assert.Throws<ArgumentException>(() => rope.Concat(null));
    }

    [Fact]
    public void Iterate_IteratesOverEachCharacterInRope()
    {
        var rope = new Rope("hello");
        var result = "";
        rope.Iterate(c => result += c);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Performance_LargeInput_HandlesInputEfficiently()
    {
        var rope = new Rope(string.Empty);
        var random = new Random(42);
        const int insertions = 10000;

        for (var i = 0; i < insertions; i++)
        {
            var length = rope.Length;
            var index = random.Next(0, length + 1);
            rope.Insert(index, "x");
        }

        Assert.Equal(insertions, rope.Length);
    }

    [Fact]
    public void EmptyString_ReturnsEmptyRope()
    {
        var rope = new Rope(string.Empty);
        Assert.Equal(0, rope.Length);
    }

    [Fact]
    public void LargeInput_HandlesInputEfficiently()
    {
        var input = new string('a', 1000000);
        var rope = new Rope(input);
        Assert.Equal(input, rope.ToString());
    }

    [Fact]
    public void ConcurrentAccess_IsThreadSafe()
    {
        var rope = new Rope("a".PadLeft(100, 'a'));
        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            var localI = i;
            tasks.Add(Task.Run(() =>
            {
                rope.Insert(localI, "b");
                _output.WriteLine($"Inserted 'b' at position {localI}. Current length: {rope.Length}");
            }));

            tasks.Add(Task.Run(() =>
            {
                var length = rope.Length;
                _output.WriteLine($"Current length after possible insert: {length}");
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.Equal(200, rope.Length);
    }

    [Fact]
    public void Equality_ComparesEqualityWithOtherRopesAndStrings()
    {
        var rope1 = new Rope("hello world");
        var rope2 = new Rope("hello world");
        var rope3 = new Rope("goodbye world");
        Assert.True(rope1.Equals(rope2));
        Assert.False(rope1.Equals(rope3));
        Assert.False(rope1.Equals("hello world")); // Cast to object to ensure correct comparison
    }

    [Fact]
    public void Boundary_HandlesBoundaryValuesCorrectly()
    {
        var rope = new Rope("a");
        Assert.Equal('a', rope[0]);
        Assert.Throws<IndexOutOfRangeException>(() => rope[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => rope[1]);
        var rope2 = new Rope("ab");
        Assert.Equal('a', rope2[0]);
        Assert.Equal('b', rope2[1]);
        Assert.Throws<IndexOutOfRangeException>(() => rope2[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => rope2[2]);
    }

    [Fact]
    public void RandomInput_HandlesInputCorrectly()
    {
        var random = new Random();
        var input = new string(Enumerable.Range(0, 10000).Select(i => (char)random.Next(32, 127)).ToArray());
        var rope = new Rope(input);
        Assert.Equal(input.Length, rope.Length);
        Assert.Equal(input, rope.ToString());
    }


    [Fact]
    public void Stress_HandlesHighLoadCorrectly()
    {
        var rope = new Rope(string.Empty);
        var tasks = new List<Task>();

        for (var i = 0; i < 10000; i++)
            tasks.Add(Task.Run(() => rope.Insert(rope.Length, "x")));

        Task.WaitAll(tasks.ToArray());

        Assert.Equal(10000, rope.Length);
    }

    [Fact]
    public void Performance_Insertions()
    {
        var rope = new Rope(new string('a', 1000000));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            for (var i = 0; i < 100000; i++)
            {
                var index = i % (rope.Length + 1); // Allow insertion at the end
                rope.Insert(index, "b");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Exception occurred: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }

        stopwatch.Stop();
        _output.WriteLine($"Total Time for 100000 insertions: {stopwatch.ElapsedMilliseconds} ms");

        Assert.Equal(1100000, rope.Length);

        var finalString = rope.ToString();
        Assert.Equal(1100000, finalString.Length);
        Assert.Equal(1000000, finalString.Count(c => c == 'a'));
        Assert.Equal(100000, finalString.Count(c => c == 'b'));
    }
}