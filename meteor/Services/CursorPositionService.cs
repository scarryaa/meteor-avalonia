using System;
using System.Numerics;
using meteor.Interfaces;

namespace meteor.Services;

public class CursorPositionService : ICursorPositionService
{
    public event Action<BigInteger, BigInteger[]>? CursorPositionChanged;

    public void UpdateCursorPosition(BigInteger position, BigInteger[] lineStarts)
    {
        CursorPositionChanged?.Invoke(position, lineStarts);
    }
}