using System;
using meteor.Interfaces;

namespace meteor.Services;

public class CursorPositionService : ICursorPositionService
{
    public event Action<long, long[]>? CursorPositionChanged;

    public void UpdateCursorPosition(long position, long[] lineStarts)
    {
        CursorPositionChanged?.Invoke(position, lineStarts);
    }
}