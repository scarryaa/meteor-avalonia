using System;
using System.Collections.Generic;
using System.Linq;

namespace meteor.Models;

public class LineCache
{
    private readonly Dictionary<long, string> _cache = new();
    private const int MaxCacheSize = 1000;

    public string GetLine(long lineNumber, Func<long, string> lineProvider)
    {
        if (_cache.TryGetValue(lineNumber, out var cachedLine)) return cachedLine;

        var line = lineProvider(lineNumber);

        if (_cache.Count >= MaxCacheSize)
            // Remove oldest entry
            _cache.Remove(_cache.Keys.First());

        _cache[lineNumber] = line;
        return line;
    }

    public void InvalidateLine(long lineNumber)
    {
        _cache.Remove(lineNumber);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}