using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace meteor.Views.Models;

public class OptimizedLineCache
{
    private readonly ConcurrentDictionary<int, List<CachedLinePart>> _lineCache = new();

    public void InvalidatePart(int lineIndex, int startColumn, int endColumn)
    {
        if (_lineCache.TryGetValue(lineIndex, out var parts))
        {
            parts.RemoveAll(p => p.StartColumn < endColumn && p.EndColumn > startColumn);
            parts.Sort((a, b) => a.StartColumn.CompareTo(b.StartColumn));
        }
    }

    public List<CachedLinePart> GetOrAddLine(int lineIndex, Func<int, List<CachedLinePart>> factory)
    {
        return _lineCache.GetOrAdd(lineIndex, factory);
    }
}