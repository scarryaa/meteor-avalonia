namespace meteor.Core.Interfaces.Services;

public interface ITextMeasurer
{
    int GetIndexAtPosition(string text, double x, double y);
    (double x, double y) GetPositionAtIndex(string text, int index);
    double GetStringWidth(string text);
    double GetStringHeight(string text);
    void ClearCache();
}