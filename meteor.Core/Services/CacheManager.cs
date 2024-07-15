using System.Collections.Concurrent;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Services;

public class CacheManager : ICacheManager
{
    private readonly ConcurrentDictionary<int, IRenderedLine> _lineCache = new();

    public IRenderedLine? GetLine(int lineIndex)
    {
        _lineCache.TryGetValue(lineIndex, out var cachedLine);
        return cachedLine;
    }

    public void SetLine(int lineIndex, IRenderedLine renderedLine)
    {
        _lineCache[lineIndex] = renderedLine;
    }

    public void InvalidateLine(int lineIndex)
    {
        _lineCache.TryRemove(lineIndex, out _);
    }

    public void InvalidateLines(int startLine, int endLine)
    {
        for (var i = startLine; i <= endLine; i++) InvalidateLine(i);
    }

    public void Clear()
    {
        _lineCache.Clear();
    }
}