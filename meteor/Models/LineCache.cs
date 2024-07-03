using System;
using System.Collections.Generic;
using System.Linq;

namespace meteor.Models;

public class LineCache
{
    private readonly Dictionary<long, string> _cache = new();
    private const int MaxCacheSize = 1000;

    public string GetLine(long lineIndex, Func<long, string> lineProvider)
    {
        if (_cache.TryGetValue(lineIndex, out var cachedLine))
            return cachedLine;

        var line = lineProvider(lineIndex);
        if (_cache.Count >= MaxCacheSize)
            _cache.Remove(_cache.Keys.First());
        _cache[lineIndex] = line;
        return line;
    }

    public void InvalidateLine(long lineIndex)
    {
        _cache.Remove(lineIndex);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}