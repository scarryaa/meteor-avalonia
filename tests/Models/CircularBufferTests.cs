using meteor.Models;

namespace tests.Models;

public class CircularBufferTests
{
    [Fact]
    public void Add_ShouldAddItemsToBuffer()
    {
        var buffer = new CircularBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);

        Assert.Equal(3, buffer.Count);
    }

    [Fact]
    public void Add_ShouldOverwriteOldestItemsWhenBufferIsFull()
    {
        var buffer = new CircularBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Add(4);

        Assert.Equal(3, buffer.Count);
        Assert.Collection(buffer,
            item => Assert.Equal(2, item),
            item => Assert.Equal(3, item),
            item => Assert.Equal(4, item)
        );
    }

    [Fact]
    public void Remove_ShouldReturnAndRemoveOldestItem()
    {
        var buffer = new CircularBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);

        var removedItem = buffer.Remove();

        Assert.Equal(1, removedItem);
        Assert.Equal(2, buffer.Count);
    }

    [Fact]
    public void Remove_ShouldThrowExceptionWhenBufferIsEmpty()
    {
        var buffer = new CircularBuffer<int>(3);

        Assert.Throws<InvalidOperationException>(() => buffer.Remove());
    }

    [Fact]
    public void Clear_ShouldEmptyTheBuffer()
    {
        var buffer = new CircularBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Clear();

        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void Enumerator_ShouldEnumerateItemsInOrder()
    {
        var buffer = new CircularBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);

        var enumeratedItems = new List<int>();

        foreach (var item in buffer) enumeratedItems.Add(item);

        Assert.Collection(enumeratedItems,
            item => Assert.Equal(1, item),
            item => Assert.Equal(2, item),
            item => Assert.Equal(3, item)
        );
    }
}