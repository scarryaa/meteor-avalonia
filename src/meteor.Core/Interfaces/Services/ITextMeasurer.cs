namespace meteor.Core.Interfaces.Services;

public interface ITextMeasurer : IDisposable
{
    int GetIndexAtPosition(ITextBufferService textBufferService, double x, double y, double verticalScrollOffset,
        double horizontalScrollOffset);
    (double x, double y) GetPositionAtIndex(string text, int index);
    double GetStringWidth(string text);
    double GetStringWidth(char[] buffer, int start, int length);
    double GetStringHeight(string text);
    double GetLineHeight();
    double GetCharWidth();
    void ClearCache();
}