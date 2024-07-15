using meteor.Core.Interfaces;
using meteor.Core.Models.Events;

namespace meteor.Services;

public class CursorPositionService : ICursorPositionService
{
    public event EventHandler<CursorPositionChangedEventArgs> CursorPositionChanged;

    public void UpdateCursorPosition(int position, List<int> lineStarts, int lastLineLength)
    {
        CursorPositionChanged?.Invoke(this, new CursorPositionChangedEventArgs(position, lineStarts, lastLineLength));
    }
}