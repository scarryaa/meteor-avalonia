using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Interfaces;

public interface ICacheManager
{
    IRenderedLine? GetLine(int lineIndex);
    void SetLine(int lineIndex, IRenderedLine renderedLine);
    void InvalidateLine(int lineIndex);
    void InvalidateLines(int startLine, int endLine);
    void Clear();
}