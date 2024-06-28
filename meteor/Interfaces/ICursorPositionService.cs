using System;

namespace meteor.Interfaces;

public interface ICursorPositionService
{
    event Action<int, int[]>? CursorPositionChanged;
    void UpdateCursorPosition(int position, int[] lineStarts);
}