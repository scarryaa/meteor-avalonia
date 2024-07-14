using meteor.core.Models;

namespace meteor.core.Interfaces;

public interface ITextBuffer
{
    string Text { get; set; }
    void Insert(int position, string text);
    void Delete(int start, int length);
    char CharAt(int index);
    IReadOnlyList<TextLine> Lines { get; }
    TextLine LineAt(int index);
    int LineCount { get; }
    string GetText(int start, int length);
    string GetSelectedText();
    void DeleteSelectedText();
    void InsertTextAtCursor(string text);
    void DeleteTextAtCursor(int length);
    void SetCursorPosition(int line, int column);
    (int Line, int Column) CursorPosition { get; }
    void ClearSelection();
    void ExtendSelectionTo(int line, int column);
    int GetLineLength(int line);
    string GetFullText();
    int CalculateCursorPosition(int line, int column);
}