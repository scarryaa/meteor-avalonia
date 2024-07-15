using meteor.Core.Models.Events;

namespace meteor.Core.Interfaces;

public interface ICursorPositionService
{
    event EventHandler<CursorPositionChangedEventArgs> CursorPositionChanged;
    void UpdateCursorPosition(int cursorPosition, List<int> lineStarts, int lastLineLength);
}