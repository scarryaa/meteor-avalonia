namespace meteor.Core.Interfaces;

public interface IWordBoundaryService
{
    (int start, int end) GetWordBoundaries(ITextBuffer textBuffer, int position);
    int GetPreviousWordBoundary(ITextBuffer textBuffer, int position);
    int GetNextWordBoundary(ITextBuffer textBuffer, int position);
}