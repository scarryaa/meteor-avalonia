namespace meteor.Core.Models;

public class LineCache
{
    private readonly Dictionary<long, string> _cache = new();
    private readonly LinkedList<long> _lruList = new();
    private const int MaxCacheSize = 1000;

    public string GetLine(long lineIndex, Func<long, string> lineProvider)
    {
        if (_cache.TryGetValue(lineIndex, out var cachedLine))
        {
            UpdateLRU(lineIndex);
            return cachedLine;
        }

        var line = lineProvider(lineIndex);
        AddToCache(lineIndex, line);
        return line;
    }

    public void InvalidateLine(long lineIndex)
    {
        if (_cache.Remove(lineIndex)) _lruList.Remove(lineIndex);
    }

    public void InvalidateRange(long startLine, long endLine)
    {
        for (var i = startLine; i <= endLine; i++) InvalidateLine(i);
    }

    public void Clear()
    {
        _cache.Clear();
        _lruList.Clear();
    }

    private void AddToCache(long lineIndex, string line)
    {
        if (_cache.Count >= MaxCacheSize)
        {
            var leastUsed = _lruList.Last.Value;
            _cache.Remove(leastUsed);
            _lruList.RemoveLast();
        }

        _cache[lineIndex] = line;
        _lruList.AddFirst(lineIndex);
    }

    private void UpdateLRU(long lineIndex)
    {
        _lruList.Remove(lineIndex);
        _lruList.AddFirst(lineIndex);
    }
}