using System;
using System.Numerics;

namespace meteor.Interfaces;

public interface ICursorPositionService
{
    event Action<long, long[]>? CursorPositionChanged;
    void UpdateCursorPosition(long cursorPosition, long[] lineStarts);
}