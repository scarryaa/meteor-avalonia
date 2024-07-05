using System;
using System.Collections.Generic;
using meteor.Interfaces;

namespace meteor.Services;

public class CursorPositionService : ICursorPositionService
{
    public event Action<long, List<long>, long>? CursorPositionChanged;

    public void UpdateCursorPosition(long position, List<long> lineStarts, long lastLineLength)
    {
        CursorPositionChanged?.Invoke(position, lineStarts, lastLineLength);
    }
}