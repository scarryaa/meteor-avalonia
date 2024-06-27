using System;
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

        public Node(string text)
        {
            Text = text;
            Length = text.Length;
            LineCount = text.Count(c => c == '\n') + 1;
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
        var sb = new StringBuilder();
        GetText(root, sb);
        return sb.ToString();
    }

    private void GetText(Node node, StringBuilder sb)
    {
        if (node == null)
            return;

        if (node.Text != null)
        {
            sb.Append(node.Text);
            return;
        }

        GetText(node.Left, sb);
        GetText(node.Right, sb);
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
            // Count lines and find the start of the target line
            var currentLineIndex = 0;
            var lineStart = 0;
            for (var j = 0; j < node.Text.Length; j++)
                if (node.Text[j] == '\n')
                {
                    if (currentLineIndex == lineIndex)
                    {
                        sb.Append(node.Text.Substring(lineStart, j - lineStart));
                        return true;
                    }

                    currentLineIndex++;
                    lineStart = j + 1;
                }

            // If the target line is the last line (without a trailing newline)
            if (currentLineIndex == lineIndex)
            {
                sb.Append(node.Text.Substring(lineStart));
                return true;
            }

            lineIndex -= currentLineIndex + 1;
            return false;
        }

        // Search in the left subtree
        if (GetLineText(node.Left, ref lineIndex, sb))
            return true;

        // Search in the right subtree
        return GetLineText(node.Right, ref lineIndex, sb);
    }
}