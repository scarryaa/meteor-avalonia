namespace meteor.Core.Interfaces.Services;

public interface ITextAnalysisService
{
    (int start, int end) GetWordBoundaries(ITextBufferService textBuffer, int index);
    (int start, int end) GetLineBoundaries(ITextBufferService textBuffer, int index);
    int GetPositionAbove(ITextBufferService textBuffer, int index);
    int GetPositionBelow(ITextBufferService textBuffer, int index);
    int GetWordStart(ITextBufferService textBuffer, int index);
    int GetWordEnd(ITextBufferService textBuffer, int index);
    int GetLineStart(ITextBufferService textBuffer, int index);
    int GetLineEnd(ITextBufferService textBuffer, int index);
}