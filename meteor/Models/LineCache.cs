using System;
using System.Collections.Generic;
using System.Linq;

namespace meteor.Models;

public class LineCache
{
    private readonly Dictionary<long, string> _cache = new();
    private readonly object _cacheLock = new();
    private const int MaxCacheSize = 1000;

    public string GetLine(long lineNumber, Func<long, string> lineProvider)
    {
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(lineNumber, out var cachedLine))
                return cachedLine;

            var line = lineProvider(lineNumber);

            if (_cache.Count >= MaxCacheSize)
                _cache.Remove(_cache.Keys.First());

            _cache[lineNumber] = line;
            return line;
        }
    }

    public void InvalidateLine(long lineNumber)
    {
        lock (_cacheLock)
        {
            var keysToRemove = _cache.Keys.Where(key => key >= lineNumber).ToList();
            foreach (var key in keysToRemove) _cache.Remove(key);
        }
    }

    public void Clear()
    {
        lock (_cacheLock)
        {
            _cache.Clear();
        }
    }

    public void AddLine(long lineNumber, string line)
    {
        lock (_cacheLock)
        {
            _cache[lineNumber] = line;
        }
    }
}