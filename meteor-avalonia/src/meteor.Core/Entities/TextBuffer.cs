using System.Collections.Concurrent;
using System.Text;
using meteor.Core.Interfaces;

namespace meteor.Core.Entities;

public class TextBuffer : ITextBuffer
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<(int Index, string Text)> _insertQueue = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Task _processTask;
    private Rope _rope;
    private bool _disposed;
    private int _pendingInsertionsLength;

    public TextBuffer(string initialText = "")
    {
        _rope = new Rope(initialText);
        _processTask = Task.Run(ProcessInsertionsAsync);
    }

    public int Length => _rope.Length + _pendingInsertionsLength;

    public char this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            // Fast path: if there are no pending insertions, directly access the rope
            if (_pendingInsertionsLength == 0)
                return _rope[index];

            // Slow path: process queued insertions and then access
            lock (_lock)
            {
                ProcessQueuedInsertions();
                return _rope[index];
            }
        }
    }

    public void GetTextSegment(int start, int length, StringBuilder output)
    {
        EnsureNotDisposed();
        if (start < 0 || length < 0 || start > Length)
            throw new ArgumentOutOfRangeException($"Invalid range: start={start}, length={length}");

        _lock.EnterReadLock();
        try
        {
            ProcessQueuedInsertions();
            if (length == 0 || Length == 0)
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
        EnsureNotDisposed();
        if (start < 0 || length < 0 || output == null || start >= Length)
            throw new ArgumentOutOfRangeException(
                $"Invalid arguments: start={start}, length={length}, output={output}");

        _lock.EnterReadLock();
        try
        {
            ProcessQueuedInsertions();
            if (length == 0 || Length == 0)
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
        EnsureNotDisposed();
        if (string.IsNullOrEmpty(text)) return;
        if (index < 0 || index > Length) throw new ArgumentOutOfRangeException(nameof(index));

        _lock.EnterWriteLock();
        try
        {
            _rope.Insert(index, text);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Delete(int index, int length)
    {
        EnsureNotDisposed();
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
        EnsureNotDisposed();
        if (start < 0 || length < 0 || start > Length)
            throw new ArgumentOutOfRangeException($"Invalid range: start={start}, length={length}");

        _lock.EnterReadLock();
        try
        {
            ProcessQueuedInsertions();
            if (length == 0 || Length == 0)
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
        return Substring(0, Length);
    }

    public string GetText(int start, int length)
    {
        return Substring(start, length);
    }

    public void ReplaceAll(string newText)
    {
        EnsureNotDisposed();
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
        EnsureNotDisposed();
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
        EnsureNotDisposed();
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
            if (_insertQueue.IsEmpty)
            {
                await Task.Delay(1, _cts.Token);
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

    public void EnsureAllInsertionsProcessed()
    {
        EnsureNotDisposed();
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

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TextBuffer));
    }
}