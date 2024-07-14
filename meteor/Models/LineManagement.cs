using System.Collections.Generic;

namespace meteor.Models;

public class LineManagement
{
    private List<Line> _lines;
    private AVLTree _rope;

    public LineManagement(AVLTree rope)
    {
        _rope = rope;
        _lines = new List<Line>();
        BuildLineTree();
    }

    private void BuildLineTree()
    {
        _lines.Clear();
        if (_rope.Length == 0)
        {
            InsertLine(0, 0);
            return;
        }

        var lineStart = 0;
        var text = _rope.GetText();
        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];
            if (character == '\n')
            {
                InsertLine(lineStart, i - lineStart + 1);
                lineStart = i + 1;
            }
        }

        if (lineStart < text.Length || lineStart == 0)
        {
            InsertLine(lineStart, text.Length - lineStart);
        }
    }

    private void InsertLine(int start, int length)
    {
        _lines.Add(new Line { Start = start, Length = length });
    }

    public int GetLineIndexFromPosition(int position)
    {
        int cumulativeLength = 0;
        for (int i = 0; i < _lines.Count; i++)
        {
            cumulativeLength += _lines[i].Length;
            if (position < cumulativeLength)
            {
                return i;
            }
        }
        return _lines.Count - 1;
    }

    public int GetLineStartIndex(int lineIndex)
    {
        int startIndex = 0;
        for (int i = 0; i < lineIndex; i++)
        {
            startIndex += _lines[i].Length;
        }
        return startIndex;
    }

    public List<string> GetLines()
    {
        var lines = new List<string>();
        var text = _rope.GetText();
        foreach (var line in _lines)
        {
            lines.Add(text.Substring(line.Start, line.Length));
        }
        return lines;
    }

    public void UpdateText(AVLTree rope)
    {
        _rope = rope;
        BuildLineTree();
    }

    private class Line
    {
        public int Start { get; set; }
        public int Length { get; set; }
    }
}