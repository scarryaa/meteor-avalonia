using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Rope
{
    private const int SPLIT_LENGTH = 1024;
    private const int MAX_NODE_LENGTH = 2048; // Example threshold for balancing

    private Node root;

    public Rope(string text)
    {
        root = Build(text);
    }

    public int Length => root?.Length ?? 0;
    public int LineCount => root?.LineCount ?? 0;

    private Node Build(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new Node(string.Empty);

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

    public int GetLineIndexFromPosition(int position)
    {
        if (position < 0 || position > Length)
            throw new ArgumentOutOfRangeException(nameof(position), "Position is out of range");

        return GetLineIndexFromPosition(root, position);
    }

    private int GetLineIndexFromPosition(Node node, int position)
    {
        if (node == null)
            return 0;

        if (node.Text != null)
        {
            var lineIndex = node.LinePositions.BinarySearch(position);
            if (lineIndex < 0)
                lineIndex = ~lineIndex - 1;
            return lineIndex;
        }

        if (position < node.Left.Length)
            return GetLineIndexFromPosition(node.Left, position);

        return node.Left.LineCount + GetLineIndexFromPosition(node.Right, position - node.Left.Length);
    }

    public int GetLineStartPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index is out of range");

        var position = 0;
        GetLineStartPosition(root, ref lineIndex, ref position);
        return position;
    }

    public int GetLineEndPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index is out of range");

        if (lineIndex == LineCount - 1)
            return Length;

        return GetLineStartPosition(lineIndex + 1) - 1;
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
        var length = end - start;
        return length;
    }

    public void Insert(int index, string text)
    {
        index = Math.Max(0, Math.Min(index, Length));
        root = Insert(root, index, text);
        root = Balance(root); // Ensure the tree is balanced after insertion
    }

    private Node Insert(Node node, int index, string text)
    {
        if (node == null)
            return new Node(text);

        if (node.Text != null)
        {
            var sb = new StringBuilder(node.Text);
            sb.Insert(index, text);
            node.Text = sb.ToString();
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

        node.Length = node.Left.Length + (node.Right?.Length ?? 0);
        node.LineCount = node.Left.LineCount + (node.Right?.LineCount ?? 0);
        return node;
    }

    public void Delete(int start, int length)
    {
        start = Math.Max(0, Math.Min(start, Length));
        length = Math.Max(0, Math.Min(length, Length - start));

        if (length > 0)
        {
            root = Delete(root, start, length);
            root = Balance(root); // Ensure the tree is balanced after deletion
        }
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

        if (length > 0 && node.Right != null)
            node.Right = Delete(node.Right, start, length);

        if (node.Left == null)
            return node.Right;
        if (node.Right == null)
            return node.Left;

        node.Length = node.Left.Length + (node.Right?.Length ?? 0);
        node.LineCount = node.Left.LineCount + (node.Right?.LineCount ?? 0);
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

    public IEnumerable<string> GetLines()
    {
        return GetLines(root);
    }

    private IEnumerable<string> GetLines(Node node)
    {
        if (node == null)
            yield break;

        if (node.Text != null)
        {
            var lines = node.Text.Split('\n');
            for (var i = 0; i < lines.Length - 1; i++)
                yield return lines[i] + "\n";
            yield return lines[^1];
        }
        else
        {
            foreach (var line in GetLines(node.Left))
                yield return line;

            foreach (var line in GetLines(node.Right))
                yield return line;
        }
    }

    public bool IsLineSelected(int lineIndex, long selectionStart, long selectionEnd)
    {
        var lineStart = GetLineStartPosition(lineIndex);
        var lineEnd = GetLineEndPosition(lineIndex);
        return (lineStart >= selectionStart && lineStart < selectionEnd) ||
               (lineEnd > selectionStart && lineEnd <= selectionEnd) ||
               (lineStart <= selectionStart && lineEnd >= selectionEnd);
    }

    public string GetLineText(int i)
    {
        if (i < 0 || i >= GetLineCount())
            throw new ArgumentOutOfRangeException(nameof(i), $"Line index is out of range {i}");

        if (root == null)
            return string.Empty;

        return GetLineTextInternal(root, i);
    }

    private string GetLineTextInternal(Node node, int lineIndex)
    {
        if (node == null)
            return string.Empty;

        if (node.Text != null)
        {
            if (lineIndex < node.LinePositions.Count - 1)
            {
                var lineStart = node.LinePositions[lineIndex];
                var lineEnd = node.LinePositions[lineIndex + 1] - 1;
                return node.Text.Substring(lineStart, lineEnd - lineStart);
            }

            if (lineIndex == node.LinePositions.Count - 1)
            {
                var lineStart = node.LinePositions[lineIndex];
                return node.Text.Substring(lineStart);
            }

            return string.Empty;
        }

        var leftResult = GetLineTextInternal(node.Left, lineIndex);
        if (!string.IsNullOrEmpty(leftResult))
            return leftResult;

        return GetLineTextInternal(node.Right, lineIndex - node.Left.LineCount);
    }

    public string GetText(int start, int length)
    {
        if (start < 0 || start >= Length)
            throw new ArgumentOutOfRangeException(nameof(start), "Start index is out of range");
        if (length < 0 || start + length > Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Length is out of range");

        // Preallocate StringBuilder with the exact capacity needed
        var sb = new StringBuilder(length);
        GetText(root, start, length, sb);
        return sb.ToString();
    }

    private void GetText(Node node, int start, int length, StringBuilder sb)
    {
        if (node == null || length == 0)
            return;

        if (node.Text != null)
        {
            // If this node contains the entire requested range, append it directly
            if (start == 0 && length == node.Text.Length)
                sb.Append(node.Text);
            else
                // Otherwise, append only the requested portion
                sb.Append(node.Text, start, Math.Min(length, node.Text.Length - start));
            return;
        }

        if (start < node.Left.Length)
        {
            var leftLength = Math.Min(length, node.Left.Length - start);
            GetText(node.Left, start, leftLength, sb);
            length -= leftLength;
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
            {
                startIndex = 0;
                return index;
            }

            startIndex -= node.Text.Length;
            return -1;
        }

        if (startIndex < node.Left.Length)
        {
            var index = IndexOf(node.Left, value, ref startIndex);
            if (index != -1)
                return index;

            startIndex = 0;
        }
        else
        {
            startIndex -= node.Left.Length;
        }

        return IndexOf(node.Right, value, ref startIndex);
    }

    private Node Balance(Node node)
    {
        if (node == null)
            return null;

        // Check if left subtree is heavier
        if (NodeSize(node.Left) > MAX_NODE_LENGTH)
        {
            // Check if left-left case
            if (NodeSize(node.Left.Left) > NodeSize(node.Left.Right))
            {
                // Perform right rotation
                return RotateRight(node);
            }

            // Perform left-right rotation
            node.Left = RotateLeft(node.Left);
            return RotateRight(node);
        }
        // Check if right subtree is heavier

        if (NodeSize(node.Right) > MAX_NODE_LENGTH)
        {
            // Check if right-right case
            if (NodeSize(node.Right.Right) > NodeSize(node.Right.Left))
            {
                // Perform left rotation
                return RotateLeft(node);
            }
            else
            {
                // Perform right-left rotation
                node.Right = RotateRight(node.Right);
                return RotateLeft(node);
            }
        }

        // If no balancing needed, return the node itself
        return node;
    }

    private int NodeSize(Node node)
    {
        return node?.Length ?? 0;
    }

    private Node RotateLeft(Node node)
    {
        if (node == null || node.Right == null)
            return node;

        var newRoot = node.Right;
        node.Right = newRoot.Left;
        newRoot.Left = node;

        // Recalculate properties like Length, LineCount, LinePositions
        RecalculateNodeProperties(node);
        RecalculateNodeProperties(newRoot);

        return newRoot;
    }

    private Node RotateRight(Node node)
    {
        if (node == null || node.Left == null)
            return node;

        var newRoot = node.Left;
        node.Left = newRoot.Right;
        newRoot.Right = node;

        // Recalculate properties like Length, LineCount, LinePositions
        RecalculateNodeProperties(node);
        RecalculateNodeProperties(newRoot);

        return newRoot;
    }

    private void RecalculateNodeProperties(Node node)
    {
        if (node == null)
            return;

        node.Length = (node.Left?.Length ?? 0) + (node.Right?.Length ?? 0);
        node.LineCount = (node.Left?.LineCount ?? 0) + (node.Right?.LineCount ?? 0);
        node.LinePositions.Clear();
        node.CalculateLinePositions();
    }

    private class Node
    {
        public string Text { get; set; }
        public int Length { get; set; }
        public int LineCount { get; set; }
        public List<int> LinePositions { get; } = new();
        public Node Left { get; set; }
        public Node Right { get; set; }

        public Node(string text)
        {
            Text = text;
            if (text != null)
            {
                Length = text.Length;
                LineCount = text.Count(c => c == '\n') + 1;
                CalculateLinePositions();
            }
        }

        public void CalculateLinePositions()
        {
            if (Text != null)
            {
                var position = 0;
                var lines = Text.Split('\n');
                foreach (var line in lines)
                {
                    LinePositions.Add(position);
                    position += line.Length + 1;
                }
            }
            else
            {
                if (Left != null)
                    Left.CalculateLinePositions();

                if (Right != null)
                    Right.CalculateLinePositions();
            }
        }
    }
}
