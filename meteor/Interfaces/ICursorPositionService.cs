using System;
using System.Numerics;

namespace meteor.Interfaces;

public interface ICursorPositionService
{
    event Action<BigInteger, BigInteger[]>? CursorPositionChanged;
    void UpdateCursorPosition(BigInteger cursorPosition, BigInteger[] lineStarts);
}