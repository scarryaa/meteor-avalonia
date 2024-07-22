using System.Collections.Concurrent;
using System.Text;
using meteor.Core.Interfaces;
using meteor.Core.Models;

namespace meteor.Core.Entities;

public class TextBuffer : ITextBuffer
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<(int Index, string Text)> _insertQueue = new();
    private readonly object _lock = new();
    private readonly Task _processTask;
    private Rope _rope;
    private bool _disposed;
    private long _pendingInsertionsLength;

    public TextBuffer(string initialText = "")
    {
        _rope = new Rope(initialText);
        _processTask = Task.Run(ProcessInsertionsAsync);
    }

    public int Length => _rope.Length + (int)Interlocked.Read(ref _pendingInsertionsLength);

    public char this[int index]
    {
        get
        {
            lock (_lock)
            {
                ProcessQueuedInsertions();
                return _rope[index];
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
            output.Clear();
            output.Append(_rope.Substring(start, Math.Min(length, _rope.Length - start)));
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
            var segment = _rope.Substring(start, Math.Min(length, Math.Min(_rope.Length - start, output.Length)));
            segment.CopyTo(0, output, 0, segment.Length);
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
            ProcessQueuedInsertions();
            _rope.Delete(index, length);
        }
    }

    public string Substring(int start, int length)
    {
        lock (_lock)
        {
            ProcessQueuedInsertions();
            return _rope.Substring(start, length);
        }
    }

    public string GetText()
    {
        lock (_lock)
        {
            ProcessQueuedInsertions();
            return _rope.ToString();
        }
    }

    public string GetText(int start, int length)
    {
        return Substring(start, length);
    }

    public void ReplaceAll(string newText)
    {
        lock (_lock)
        {
            _insertQueue.Clear();
            Interlocked.Exchange(ref _pendingInsertionsLength, 0);
            _rope = new Rope(newText);
        }
    }

    public void Iterate(Action<char> action)
    {
        lock (_lock)
        {
            ProcessQueuedInsertions();
            _rope.Iterate(action);
        }
    }

    public void IndexedIterate(Action<char, int> action)
    {
        lock (_lock)
        {
            ProcessQueuedInsertions();
            var index = 0;
            _rope.Iterate(c =>
            {
                action(c, index);
                index++;
            });
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
            _rope.Insert(index, text);
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