using meteor.core.Interfaces;
using meteor.core.Utils;

namespace meteor.core.Models;

public class TextBuffer : ITextBuffer
{
    private Rope _rope;
    private List<TextLine> _lines;
    private bool _isDirty;
    private int _cursorPosition;
    private (int Line, int Column) _selectionStart;
    private bool _hasSelection;

    public TextBuffer(string initialText = "")
    {
        _rope = new Rope(initialText);
        _lines = new List<TextLine> { new(0, 0, 0) };
        _isDirty = true;
        _cursorPosition = 0;
        CursorPosition = (0, 0);
        _selectionStart = (0, 0);
        _hasSelection = false;
        EnsureMinimumLine();
    }

    public string Text
    {
        get => _rope.ToString();
        set
        {
            _rope = new Rope(value);
            _isDirty = true;
            EnsureMinimumLine();
        }
    }

    public void Insert(int position, string text)
    {
        _rope = _rope.Insert(position, text);
        _isDirty = true;
        EnsureMinimumLine();
    }

    public void Delete(int start, int length)
    {
        _rope = _rope.Delete(start, length);
        _isDirty = true;
        EnsureMinimumLine();
    }

    public char CharAt(int index)
    {
        return _rope[index];
    }

    public IReadOnlyList<TextLine> Lines
    {
        get
        {
            if (_isDirty || _lines == null) UpdateLines();
            return _lines;
        }
    }

    private void UpdateLines()
    {
        _lines = new List<TextLine>();
        var lineStart = 0;
        var lineNumber = 0;

        for (var i = 0; i < _rope.Length; i++)
            if (_rope[i] == '\n')
            {
                _lines.Add(new TextLine(lineNumber, lineStart, i - lineStart + 1));
                lineStart = i + 1;
                lineNumber++;
            }

        if (lineStart < _rope.Length || _rope.Length == 0)
            _lines.Add(new TextLine(lineNumber, lineStart, _rope.Length - lineStart));

        EnsureMinimumLine();
        _isDirty = false;
    }

    private void EnsureMinimumLine()
    {
        if (_lines == null || _lines.Count == 0) _lines = new List<TextLine> { new(0, 0, 0) };
    }

    public TextLine LineAt(int index)
    {
        return Lines[index];
    }

    public int LineCount => Lines.Count;

    public string GetText(int start, int length)
    {
        return _rope.Substring(start, length);
    }

    public string GetSelectedText()
    {
        if (!_hasSelection) return string.Empty;

        var (startLine, startColumn) = _selectionStart;
        var (endLine, endColumn) = CursorPosition;

        if (startLine > endLine || (startLine == endLine && startColumn > endColumn))
        {
            (startLine, endLine) = (endLine, startLine);
            (startColumn, endColumn) = (endColumn, startColumn);
        }

        var start = CalculateCursorPosition(startLine, startColumn);
        var end = CalculateCursorPosition(endLine, endColumn);
        return GetText(start, end - start);
    }

    public void DeleteSelectedText()
    {
        if (!_hasSelection) return;

        var (startLine, startColumn) = _selectionStart;
        var (endLine, endColumn) = CursorPosition;

        if (startLine > endLine || (startLine == endLine && startColumn > endColumn))
        {
            (startLine, endLine) = (endLine, startLine);
            (startColumn, endColumn) = (endColumn, startColumn);
        }

        var start = CalculateCursorPosition(startLine, startColumn);
        var end = CalculateCursorPosition(endLine, endColumn);
        Delete(start, end - start);
        ClearSelection();
        SetCursorPosition(startLine, startColumn);
    }

    public void InsertTextAtCursor(string text)
    {
        if (_hasSelection) DeleteSelectedText();
        Insert(_cursorPosition, text);
        _cursorPosition += text.Length;
        CursorPosition = CalculateLineAndColumn(_cursorPosition);
    }

    public void DeleteTextAtCursor(int length)
    {
        if (_cursorPosition + length <= _rope.Length) Delete(_cursorPosition, length);
    }

    public void SetCursorPosition(int line, int column)
    {
        if (line < 0 || line >= Lines.Count)
            throw new ArgumentOutOfRangeException(nameof(line), "Line must be within the valid range.");
        if (column < 0 || column > Lines[line].Length)
            throw new ArgumentOutOfRangeException(nameof(column), "Column must be within the valid range.");

        CursorPosition = (line, column);
        _cursorPosition = CalculateCursorPosition(line, column);
    }

    public (int Line, int Column) CursorPosition { get; private set; }

    public int CalculateCursorPosition(int line, int column)
    {
        var position = 0;
        for (var i = 0; i < line; i++) position += Lines[i].Length;
        return position + column;
    }

    private (int Line, int Column) CalculateLineAndColumn(int position)
    {
        var currentPos = 0;
        for (var i = 0; i < Lines.Count; i++)
        {
            if (currentPos + Lines[i].Length > position) return (i, position - currentPos);
            currentPos += Lines[i].Length;
        }

        return (Lines.Count - 1, Lines[^1].Length);
    }

    public void ClearSelection()
    {
        _hasSelection = false;
        _selectionStart = CursorPosition;
    }

    public void ExtendSelectionTo(int line, int column)
    {
        if (!_hasSelection)
        {
            _selectionStart = CursorPosition;
            _hasSelection = true;
        }

        SetCursorPosition(line, column);
    }

    public int GetLineLength(int line)
    {
        return Lines[line].Length;
    }

    public string GetFullText()
    {
        return _rope.ToString();
    }
}