using System.Collections.Concurrent;
using System.Text;
using meteor.Core.Interfaces;

namespace meteor.Core.Entities;

public class TextBuffer : ITextBuffer
{
    private readonly StringBuilder _buffer;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<(int Index, string Text)> _insertQueue = new();
    private readonly object _lock = new();
    private readonly Task _processTask;
    private bool _disposed;
    private long _pendingInsertionsLength;

    public TextBuffer(string initialText = "")
    {
        _buffer = new StringBuilder(initialText);
        _processTask = Task.Run(ProcessInsertionsAsync);
    }

    public int Length => _buffer.Length + (int)Interlocked.Read(ref _pendingInsertionsLength);

    public char this[int index]
    {
        get
        {
            lock (_lock)
            {
                ProcessQueuedInsertions();
                return _buffer[index];
            }
        }
    }

    public void GetTextSegment(int start, int length, StringBuilder output)
    {
        if (start < 0 || length < 0)
            throw new ArgumentOutOfRangeException($"Invalid range: start={start}, length={length}");

        lock (_lock)
        {
            ProcessQueuedInsertions();

            // Ensure we don't go out of bounds
            var actualLength = Math.Min(length, _buffer.Length - start);
            if (actualLength <= 0)
                return;

            output.Clear();
            output.Append(_buffer, start, actualLength);
        }
    }

    public void GetTextSegment(int start, int length, char[] output)
    {
        if (start < 0 || length < 0 || output == null)
            throw new ArgumentOutOfRangeException(
                $"Invalid arguments: start={start}, length={length}, output={output}");

        lock (_lock)
        {
            ProcessQueuedInsertions();

            // Ensure we don't go out of bounds
            var actualLength = Math.Min(Math.Min(length, output.Length), _buffer.Length - start);
            if (actualLength <= 0)
                return;

            for (var i = 0; i < actualLength; i++)
                output[i] = _buffer[start + i];
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
        lock (_lock)
        {
            if (index < 0 || index >= _buffer.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (length < 0 || index + length > _buffer.Length) throw new ArgumentOutOfRangeException(nameof(length));
            ProcessQueuedInsertions();
            _buffer.Remove(index, length);
        }
    }

    public string Substring(int start, int length)
    {
        lock (_lock)
        {
            if (start < 0 || start >= _buffer.Length) throw new ArgumentOutOfRangeException(nameof(start));
            if (length < 0 || start + length > _buffer.Length) throw new ArgumentOutOfRangeException(nameof(length));
            ProcessQueuedInsertions();
            return _buffer.ToString(start, length);
        }
    }

    public string GetText()
    {
        lock (_lock)
        {
            ProcessQueuedInsertions();
            return _buffer.ToString();
        }
    }

    public string GetText(int start, int length)
    {
        lock (_lock)
        {
            ProcessQueuedInsertions();
            return _buffer.ToString(start, length);
        }
    }

    public void ReplaceAll(string newText)
    {
        lock (_lock)
        {
            _insertQueue.Clear();
            Interlocked.Exchange(ref _pendingInsertionsLength, 0);
            _buffer.Clear();
            _buffer.Append(newText);
        }
    }

    public void Iterate(Action<char> action)
    {
        lock (_lock)
        {
            ProcessQueuedInsertions();
            for (var i = 0; i < _buffer.Length; i++) action(_buffer[i]);
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

            lock (_lock)
            {
                ProcessQueuedInsertions();
            }

            _cts.Dispose();
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
            // Ensure the index is within valid bounds
            index = Math.Max(0, Math.Min(index, _buffer.Length));
            _buffer.Insert(index, text);
            Interlocked.Add(ref _pendingInsertionsLength, -text.Length);
        }
    }

    private async Task ProcessInsertionsAsync()
    {
        while (!_cts.IsCancellationRequested)
            if (_insertQueue.IsEmpty)
                await Task.Delay(1);
            else
                lock (_lock)
                {
                    ProcessQueuedInsertions();
                }
    }

    public void EnsureAllInsertionsProcessed()
    {
        lock (_lock)
        {
            ProcessQueuedInsertions();
        }
    }
}