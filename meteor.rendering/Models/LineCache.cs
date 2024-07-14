using meteor.core.Models;

namespace meteor.rendering.Models;

public class LineCache
{
    private readonly Dictionary<int, CachedLine> _cache = new();
    private const int MaxCacheSize = 1000;

    public CachedLine GetLine(int lineIndex, string lineText)
    {
        if (_cache.TryGetValue(lineIndex, out var cachedLine) && cachedLine.Text == lineText) return cachedLine;
        return null;
    }

    public void CacheLine(int lineIndex, CachedLine line)
    {
        if (_cache.Count >= MaxCacheSize)
        {
            // Simple eviction strategy: remove the oldest item
            var oldestLineIndex = -1;
            var oldestTimestamp = DateTime.MaxValue;
            foreach (var kvp in _cache)
                if (kvp.Value.Timestamp < oldestTimestamp)
                {
                    oldestLineIndex = kvp.Key;
                    oldestTimestamp = kvp.Value.Timestamp;
                }

            if (oldestLineIndex != -1) _cache.Remove(oldestLineIndex);
        }

        _cache[lineIndex] = line;
    }

    public void Invalidate(int startLine, int endLine)
    {
        for (var i = startLine; i <= endLine; i++) _cache.Remove(i);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}