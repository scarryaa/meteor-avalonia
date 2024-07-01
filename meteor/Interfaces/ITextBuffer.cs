using System;
using System.Collections.Generic;

namespace meteor.Interfaces;

public interface ITextBuffer
{
    event EventHandler TextChanged;
    event EventHandler LinesUpdated;
    string Text { get; }
    long Length { get; }
    double TotalHeight { get; }
    List<long> LineStarts { get; }
    Rope Rope { get; }
    long LineCount { get; }
    long LongestLineLength { get; }
    double LineHeight { get; set; }
    void InsertText(long position, string text);
    void DeleteText(long start, long length);
    void SetText(string newText);
    void Clear();
    void UpdateLineCache();
    string GetLineText(long lineIndex);
    bool IsLineSelected(int lineIndex, long selectionStart, long selectionEnd);
    long GetLineStartPosition(int lineIndex);
    long GetLineEndPosition(int lineIndex);
    long GetLineLength(long lineIndex);
    long GetLineIndexFromPosition(long position);
}