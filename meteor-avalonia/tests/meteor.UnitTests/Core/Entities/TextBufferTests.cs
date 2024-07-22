using System.Collections.Concurrent;
using System.Text;
using meteor.Core.Interfaces;
using meteor.Core.Models;

namespace meteor.UnitTests.Core.Entities;

public class TextBuffer : ITextBuffer
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<(int Index, string Text)> _insertQueue = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Task _processTask;
    private Rope _rope;
    private bool _disposed;
    private long _pendingInsertionsLength;

    public TextBuffer(string? initialText = "")
    {
        _rope = new Rope(initialText);
        _processTask = Task.Run(ProcessInsertionsAsync);
    }

    public int Length
    {
        get
        {
            var length = _rope.Length + (int)Interlocked.Read(ref _pendingInsertionsLength);
            Console.WriteLine($"Current Length: {length}");
            return length;
        }
    }

    public char this[int index]
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                ProcessQueuedInsertions();
                return _rope[index];
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public void GetTextSegment(int start, int length, StringBuilder output)
    {
        if (start < 0 || length < 0 || start >= Length)
            throw new ArgumentOutOfRangeException($"Invalid range: start={start}, length={length}");

        _lock.EnterReadLock();
        try
        {
            ProcessQueuedInsertions();
            if (length == 0)
            {
                output.Clear();
                return;
            }

            var validLength = Math.Min(length, _rope.Length - start);
            output.Clear();
            output.Append(_rope.Substring(start, validLength));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void GetTextSegment(int start, int length, char[] output)
    {
        if (start < 0 || length < 0 || output == null || start >= Length)
            throw new ArgumentOutOfRangeException(
                $"Invalid arguments: start={start}, length={length}, output={output}");

        _lock.EnterReadLock();
        try
        {
            ProcessQueuedInsertions();
            if (length == 0)
            {
                Array.Clear(output, 0, output.Length);
                return;
            }

            var validLength = Math.Min(length, Math.Min(_rope.Length - start, output.Length));
            var segment = _rope.Substring(start, validLength);
            segment.CopyTo(0, output, 0, segment.Length);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Insert(int index, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (index < 0 || index > Length) throw new ArgumentOutOfRangeException(nameof(index));
        _insertQueue.Enqueue((index, text));
        Interlocked.Add(ref _pendingInsertionsLength, text.Length);
    }

    public void Delete(int index, int length)
    {
        if (index < 0 || length < 0 || index >= Length)
            throw new ArgumentOutOfRangeException($"Invalid range: index={index}, length={length}");

        _lock.EnterWriteLock();
        try
        {
            ProcessQueuedInsertions();
            _rope.Delete(index, length);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public string Substring(int start, int length)
    {
        if (start < 0 || length < 0 || start >= Length)
            throw new ArgumentOutOfRangeException($"Invalid range: start={start}, length={length}");

        _lock.EnterReadLock();
        try
        {
            ProcessQueuedInsertions();
            if (length == 0)
                return string.Empty;

            var validLength = Math.Min(length, _rope.Length - start);
            return _rope.Substring(start, validLength);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public string GetText()
    {
        _lock.EnterReadLock();
        try
        {
            ProcessQueuedInsertions();
            return _rope.ToString();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public string GetText(int start, int length)
    {
        return Substring(start, length);
    }

    public void ReplaceAll(string? newText)
    {
        _lock.EnterWriteLock();
        try
        {
            _insertQueue.Clear();
            Interlocked.Exchange(ref _pendingInsertionsLength, 0);
            _rope = new Rope(newText);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Iterate(Action<char> action)
    {
        _lock.EnterReadLock();
        try
        {
            ProcessQueuedInsertions();
            _rope.Iterate(action);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void IndexedIterate(Action<char, int> action)
    {
        _lock.EnterReadLock();
        try
        {
            ProcessQueuedInsertions();
            var index = 0;
            _rope.Iterate(c =>
            {
                action(c, index);
                index++;
            });
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _cts.Cancel();
            try
            {
                _processTask.Wait();
            }
            catch (AggregateException ae)
            {
                ae.Handle(ex => ex is TaskCanceledException);
            }

            _lock.EnterWriteLock();
            try
            {
                ProcessQueuedInsertions();
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            _cts.Dispose();
            _lock.Dispose();
        }

        _disposed = true;
    }

    ~TextBuffer()
    {
        Dispose(false);
    }

    private void ProcessQueuedInsertions()
    {
        while (_insertQueue.TryDequeue(out var insertion))
        {
            var (index, text) = insertion;
            _rope.Insert(index, text);
            Interlocked.Add(ref _pendingInsertionsLength, -text.Length);
        }
    }

    private async Task ProcessInsertionsAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (_insertQueue.IsEmpty)
            {
                await Task.Delay(1);
            }
            else
            {
                _lock.EnterWriteLock();
                try
                {
                    ProcessQueuedInsertions();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }
    }

    public void EnsureAllInsertionsProcessed()
    {
        _lock.EnterWriteLock();
        try
        {
            ProcessQueuedInsertions();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}