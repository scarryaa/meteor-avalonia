using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace meteor.Models;

public class Rope
{
    private const int SPLIT_LENGTH = 1024;

    private class Node
    {
        public string Text { get; set; }
        public int Length { get; set; }
        public int LineCount { get; set; }
        public Node Left { get; set; }
        public Node Right { get; set; }
        public List<int> LinePositions { get; }

        public Node(string text)
        {
            Text = text;
            Length = text.Length;
            LineCount = text.Count(c => c == '\n') + 1;
            LinePositions = new List<int>();
            CalculateLinePositions();
        }

        public void CalculateLinePositions()
        {
            LinePositions.Add(0); // Start of the first line
            for (var i = 0; i < Text.Length; i++)
                if (Text[i] == '\n')
                    LinePositions.Add(i + 1);
        }
    }

    private Node root;

    public Rope(string text)
    {
        root = Build(text);
    }

    private Node Build(string text)
    {
        if (text.Length <= SPLIT_LENGTH)
            return new Node(text);

        var mid = text.Length / 2;
        var node = new Node(null);
        node.Left = Build(text.Substring(0, mid));
        node.Right = Build(text.Substring(mid));
        node.Length = node.Left.Length + (node.Right?.Length ?? 0);
        node.LineCount = node.Left.LineCount + (node.Right?.LineCount ?? 0);
        return node;
    }

    public int GetLineStartPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index is out of range");

        var position = 0;
        GetLineStartPosition(root, ref lineIndex, ref position);
        return position;
    }

    private bool GetLineStartPosition(Node node, ref int lineIndex, ref int position)
    {
        if (node == null)
            return false;

        if (node.Text != null)
        {
            if (lineIndex < node.LinePositions.Count)
            {
                position += node.LinePositions[lineIndex];
                return true;
            }

            lineIndex -= node.LinePositions.Count;
            position += node.Length;
            return false;
        }

        if (GetLineStartPosition(node.Left, ref lineIndex, ref position))
            return true;

        return GetLineStartPosition(node.Right, ref lineIndex, ref position);
    }

    public int GetLineLength(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index is out of range");

        var start = GetLineStartPosition(lineIndex);
        var end = lineIndex < LineCount - 1 ? GetLineStartPosition(lineIndex + 1) : Length;

        return end - start;
    }

    public void Insert(int index, string text)
    {
        index = Math.Max(0, Math.Min(index, Length));
        root = Insert(root, index, text);
    }

    private Node Insert(Node node, int index, string text)
    {
        if (node == null)
            return new Node(text);

        if (node.Text != null)
        {
            node.Text = node.Text.Insert(index, text);
            node.Length = node.Text.Length;
            node.LineCount = node.Text.Count(c => c == '\n') + 1;
            node.LinePositions.Clear();
            node.CalculateLinePositions();
            return node;
        }

        if (index <= node.Left.Length)
            node.Left = Insert(node.Left, index, text);
        else
            node.Right = Insert(node.Right, index - node.Left.Length, text);

        node.Length = node.Left.Length + node.Right.Length;
        node.LineCount = node.Left.LineCount + node.Right.LineCount;
        return node;
    }

    public void Delete(int start, int length)
    {
        start = Math.Max(0, Math.Min(start, Length));
        length = Math.Max(0, Math.Min(length, Length - start));

        if (length > 0) root = Delete(root, start, length);
    }

    private Node Delete(Node node, int start, int length)
    {
        if (node == null)
            return null;

        if (node.Text != null)
        {
            node.Text = node.Text.Remove(start, Math.Min(length, node.Text.Length - start));
            node.Length = node.Text.Length;
            node.LineCount = node.Text.Count(c => c == '\n') + 1;
            node.LinePositions.Clear();
            node.CalculateLinePositions();
            return node.Length > 0 ? node : null;
        }

        if (start < node.Left.Length)
        {
            node.Left = Delete(node.Left, start, Math.Min(length, node.Left.Length - start));
            length -= Math.Min(length, node.Left.Length - start);
            start = 0;
        }
        else
        {
            start -= node.Left.Length;
        }

        if (length > 0 && node.Right != null) node.Right = Delete(node.Right, start, length);

        if (node.Left == null)
            return node.Right;
        if (node.Right == null)
            return node.Left;

        node.Length = node.Left.Length + node.Right.Length;
        node.LineCount = node.Left.LineCount + node.Right.LineCount;
        return node;
    }

    public int GetLineCount()
    {
        return root?.LineCount ?? 0;
    }

    public string GetText()
    {
        return GetText(0, Length);
    }

    public string GetText(int start, int length)
    {
        if (start < 0 || start >= Length)
            throw new ArgumentOutOfRangeException(nameof(start), "Start index is out of range");
        if (length < 0 || start + length > Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Length is out of range");

        var sb = new StringBuilder();
        GetText(root, start, length, sb);
        return sb.ToString();
    }

    private void GetText(Node node, int start, int length, StringBuilder sb)
    {
        if (node == null || length == 0)
            return;

        if (node.Text != null)
        {
            var end = Math.Min(start + length, node.Length);
            sb.Append(node.Text.Substring(start, end - start));
            return;
        }

        if (start < node.Left.Length)
        {
            GetText(node.Left, start, Math.Min(length, node.Left.Length - start), sb);
            length -= Math.Min(length, node.Left.Length - start);
            start = 0;
        }
        else
        {
            start -= node.Left.Length;
        }

        if (length > 0)
            GetText(node.Right, start, length, sb);
    }

    public int IndexOf(char value, int startIndex = 0)
    {
        if (startIndex < 0 || startIndex >= Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index is out of range");

        return IndexOf(root, value, ref startIndex);
    }

    private int IndexOf(Node node, char value, ref int startIndex)
    {
        if (node == null)
            return -1;

        if (node.Text != null)
        {
            var index = node.Text.IndexOf(value, startIndex);
            if (index != -1)
                return index;
            startIndex = 0;
            return -1;
        }

        if (startIndex < node.Left.Length)
        {
            var index = IndexOf(node.Left, value, ref startIndex);
            if (index != -1)
                return index;
        }
        else
        {
            startIndex -= node.Left.Length;
        }

        var rightIndex = IndexOf(node.Right, value, ref startIndex);
        if (rightIndex != -1)
            return node.Left.Length + rightIndex;

        return -1;
    }

    public int Length => root?.Length ?? 0;
    public int LineCount => root.LineCount;

    public string GetLineText(int i)
    {
        if (i < 0 || i >= GetLineCount())
            throw new ArgumentOutOfRangeException(nameof(i), "Line index is out of range");

        var sb = new StringBuilder();
        GetLineText(root, ref i, sb);
        return sb.ToString();
    }

    private bool GetLineText(Node node, ref int lineIndex, StringBuilder sb)
    {
        if (node == null)
            return false;

        if (node.Text != null)
        {
            if (lineIndex < node.LinePositions.Count - 1)
            {
                var lineStart = node.LinePositions[lineIndex];
                var lineEnd = node.LinePositions[lineIndex + 1] - 1;
                sb.Append(node.Text.Substring(lineStart, lineEnd - lineStart));
                return true;
            }

            if (lineIndex == node.LinePositions.Count - 1)
            {
                var lineStart = node.LinePositions[lineIndex];
                sb.Append(node.Text.Substring(lineStart));
                return true;
            }

            lineIndex -= node.LinePositions.Count;
            return false;
        }

        // Search in the left subtree
        if (GetLineText(node.Left, ref lineIndex, sb))
            return true;

        // Search in the right subtree
        return GetLineText(node.Right, ref lineIndex, sb);
    }
}
