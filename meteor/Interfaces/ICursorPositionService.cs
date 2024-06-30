using System;
using System.Collections.Generic;

namespace meteor.Interfaces;

public interface ICursorPositionService
{
    event Action<long, List<long>>? CursorPositionChanged;
    void UpdateCursorPosition(long cursorPosition, List<long> lineStarts);
}