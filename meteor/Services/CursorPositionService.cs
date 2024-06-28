using System;
using meteor.Interfaces;

namespace meteor.Services;

public class CursorPositionService : ICursorPositionService
{
    public event Action<int, int[]>? CursorPositionChanged;

    public void UpdateCursorPosition(int position, int[] lineStarts)
    {
        CursorPositionChanged?.Invoke(position, lineStarts);
    }
}