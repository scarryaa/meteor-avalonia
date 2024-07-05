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
    IRope Rope { get; }
    long LineCount { get; }
    long LongestLineLength { get; }
    double LineHeight { get; set; }
    (int StartLine, int EndLine) GetUpdatedRange();
    void SetLineStartPosition(int lineIndex, long position);
    string GetText(long start, long length);
    void InsertText(long position, string text);
    void DeleteText(long start, long length);
    void SetText(string newText);
    string GetTextForLines(int startLine, int endLine);
    void Clear();
    void UpdateLineCache();
    string GetLineText(long lineIndex);
    public void RaiseLinesUpdated();
    bool IsLineSelected(int lineIndex, long selectionStart, long selectionEnd);
    long GetLineStartPosition(int lineIndex);
    long GetVisualLineLength(int lineIndex);
    long GetLineEndPosition(int lineIndex);
    long GetLineLength(long lineIndex);
    long GetLineIndexFromPosition(long position);
}