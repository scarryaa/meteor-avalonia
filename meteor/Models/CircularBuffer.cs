using System;
using System.Collections;
using System.Collections.Generic;

namespace meteor.Models;

public class CircularBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _tail;

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        _buffer = new T[capacity];
        _head = 0;
        _tail = 0;
        Count = 0;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<T> GetEnumerator()
    {
        var index = _head;
        for (var i = 0; i < Count; i++)
        {
            yield return _buffer[index];
            index = (index + 1) % _buffer.Length;
        }
    }

    public void Add(T item)
    {
        _buffer[_tail] = item;
        _tail = (_tail + 1) % _buffer.Length;
        if (Count == _buffer.Length)
            _head = (_head + 1) % _buffer.Length; // Overwrite the oldest item
        else
            Count++;
    }

    public T Remove()
    {
        if (Count == 0) throw new InvalidOperationException("Buffer is empty.");
        var item = _buffer[_head];
        _head = (_head + 1) % _buffer.Length;
        Count--;
        return item;
    }

    public int Count { get; private set; }

    public void Clear()
    {
        _head = 0;
        _tail = 0;
        Count = 0;
        Array.Clear(_buffer, 0, _buffer.Length);
    }
}