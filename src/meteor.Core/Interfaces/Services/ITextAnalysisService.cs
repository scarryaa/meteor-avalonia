namespace meteor.Core.Interfaces.Services;

public interface ITextAnalysisService
{
    (int start, int end) GetWordBoundariesAt(string text, int index);
    (int start, int end) GetLineBoundariesAt(string text, int index);
    int GetPositionAbove(string text, int index);
    int GetPositionBelow(string text, int index);
}