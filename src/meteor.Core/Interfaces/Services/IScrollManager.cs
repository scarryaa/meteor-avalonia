using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services;

public interface IScrollManager
{
    Task<Vector> CalculateScrollOffsetAsync(int cursorPosition, double editorWidth, double editorHeight,
        double viewportWidth, double viewportHeight, Vector currentScrollOffset, int textLength);
}