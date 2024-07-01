using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using meteor.Interfaces;

public class Rope : IRope
{
    private const int SPLIT_LENGTH = 1024;
    private const int MAX_NODE_LENGTH = 2048;
    private Node root;
    private int _cachedLineCount = -1;
    private int _cachedLongestLineLength = -1;
    private int _cachedLongestLineIndex = -1;
    private long _changeCounter;

    public Rope(string text)
    {
        root = Build(text);
        InvalidateCache();
    }

    public int Length => root?.Length ?? 0;

    public int LineCount =>
        _cachedLineCount >= 0 ? _cachedLineCount : _cachedLineCount = Math.Max(1, CalculateLineCount());

    public int LongestLineLength => _cachedLongestLineLength >= 0
        ? _cachedLongestLineLength
        : _cachedLongestLineLength = CalculateLongestLineLength();

    public int LongestLineIndex => _cachedLongestLineIndex >= 0
        ? _cachedLongestLineIndex
        : _cachedLongestLineIndex = CalculateLongestLineIndex();

    public long GetChangeCounter()
    {
        return _changeCounter;
    }

    private Node Build(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new Node(string.Empty);

        if (text.Length <= SPLIT_LENGTH)
            return new Node(text);

        var mid = text.Length / 2;
        var node = new Node(null)
        {
            Left = Build(text.Substring(0, mid)),
            Right = Build(text.Substring(mid))
        };
        node.UpdateProperties();
        return node;
    }

    public int GetLineIndexFromPosition(int position)
    {
        if (Length == 0)
            return 0;

        var low = 0;
        var high = LineCount - 1;

        while (low <= high)
        {
            var mid = (low + high) / 2;
            var lineStart = GetLineStartPosition(mid);
            var nextLineStart = mid == LineCount - 1 ? Length : GetLineStartPosition(mid + 1);

            if (position >= lineStart && position < nextLineStart)
                return mid;

            if (position < lineStart)
                high = mid - 1;
            else
                low = mid + 1;
        }

        return LineCount - 1; // Default to last line if not found
    }

    public int GetLineStartPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index is out of range");

        return root.GetLineStartPosition(lineIndex);
    }

    public int GetLineEndPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index is out of range");

        if (lineIndex == LineCount - 1)
            return Length;

        return GetLineStartPosition(lineIndex + 1) - 1;
    }

    public int GetLineLength(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index is out of range");

        if (Length == 0) return 0;

        var start = GetLineStartPosition(lineIndex);
        var end = lineIndex < LineCount - 1 ? GetLineStartPosition(lineIndex + 1) : Length;
        return end - start;
    }

    public void Insert(int index, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        index = Math.Max(0, Math.Min(index, Length));
        root = Insert(root, index, text);
        root = Balance(root);
        InvalidateCache();
        _changeCounter++;
    }

    private Node Insert(Node node, int index, string text)
    {
        if (node == null)
            return new Node(text);

        if (node.IsLeaf)
        {
            var sb = new StringBuilder(node.Text);
            sb.Insert(index, text);
            node.Text = sb.ToString();
            node.UpdateProperties();
            return node;
        }

        if (index <= node.Left.Length)
            node.Left = Insert(node.Left, index, text);
        else
            node.Right = Insert(node.Right, index - node.Left.Length, text);

        node.UpdateProperties();
        return node;
    }

    public void Delete(int start, int length)
    {
        if (length <= 0) return;

        start = Math.Max(0, Math.Min(start, Length));
        length = Math.Min(length, Length - start);

        root = Delete(root, start, length);
        root = Balance(root);
        InvalidateCache();
        _changeCounter++;
    }

    private Node Delete(Node node, int start, int length)
    {
        if (node == null || length <= 0)
            return node;

        if (node.IsLeaf)
        {
            node.Text = node.Text.Remove(start, Math.Min(length, node.Text.Length - start));
            node.UpdateProperties();
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

        node.UpdateProperties();
        return node;
    }

    public string GetText()
    {
        return GetText(0, Length);
    }

    public string GetText(int start, int length)
    {
        if (Length == 0)
            return string.Empty;

        start = Math.Max(0, Math.Min(start, Length - 1));
        length = Math.Max(0, Math.Min(length, Length - start));

        if (length == 0)
            return string.Empty;

        var sb = new StringBuilder(length);
        root.GetText(start, length, sb);
        return sb.ToString();
    }

    public string GetLineText(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            return string.Empty;

        if (Length == 0) return string.Empty;

        var start = GetLineStartPosition(lineIndex);
        var length = GetLineLength(lineIndex);
        return GetText(start, length);
    }

    public int IndexOf(char value, int startIndex = 0)
    {
        if (startIndex < 0 || startIndex >= Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index is out of range");

        return root.IndexOf(value, startIndex);
    }

    private Node Balance(Node node)
    {
        if (node == null || node.IsLeaf)
            return node;

        if (node.Length > MAX_NODE_LENGTH)
        {
            if (node.Left != null && node.Right != null)
            {
                if (node.Left.Length > 2 * node.Right.Length)
                    return RotateRight(node);
                if (node.Right.Length > 2 * node.Left.Length)
                    return RotateLeft(node);
            }
            else if (node.Left != null && node.Left.Length > MAX_NODE_LENGTH)
            {
                return RotateRight(node);
            }
            else if (node.Right != null && node.Right.Length > MAX_NODE_LENGTH)
            {
                return RotateLeft(node);
            }
        }

        // Recursively balance children
        if (node.Left != null)
            node.Left = Balance(node.Left);
        if (node.Right != null)
            node.Right = Balance(node.Right);

        return node;
    }

    private Node RotateLeft(Node node)
    {
        if (node == null || node.Right == null)
            return node;

        var newRoot = node.Right;
        node.Right = newRoot.Left;
        newRoot.Left = node;
        node.UpdateProperties();
        newRoot.UpdateProperties();
        return newRoot;
    }

    private Node RotateRight(Node node)
    {
        if (node == null || node.Left == null)
            return node;

        var newRoot = node.Left;
        node.Left = newRoot.Right;
        newRoot.Right = node;
        node.UpdateProperties();
        newRoot.UpdateProperties();
        return newRoot;
    }

    private void InvalidateCache()
    {
        _cachedLineCount = -1;
        _cachedLongestLineLength = -1;
        _cachedLongestLineIndex = -1;
    }

    private int CalculateLineCount()
    {
        return root?.LineCount ?? 0;
    }

    private int CalculateLongestLineLength()
    {
        if (root == null) return 0;
        return root.GetLongestLine(out _cachedLongestLineIndex);
    }

    private int CalculateLongestLineIndex()
    {
        return _cachedLongestLineIndex;
    }

    public bool IsLineSelected(int lineIndex, long selectionStart, long selectionEnd)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index is out of range");

        var lineStartPosition = GetLineStartPosition(lineIndex);
        var lineEndPosition = GetLineEndPosition(lineIndex);

        // Ensure selectionStart is before selectionEnd
        if (selectionStart > selectionEnd)
            (selectionStart, selectionEnd) = (selectionEnd, selectionStart);

        // Check if the line overlaps with the selection
        return (lineStartPosition >= selectionStart && lineStartPosition < selectionEnd) ||
               (lineEndPosition > selectionStart && lineEndPosition <= selectionEnd) ||
               (lineStartPosition <= selectionStart && lineEndPosition >= selectionEnd);
    }

    private class Node
    {
        public string Text { get; set; }
        public Node Left { get; set; }
        public Node Right { get; set; }
        public int Length { get; private set; }
        public int LineCount { get; private set; }
        public List<int> LinePositions { get; } = new();
        public bool IsLeaf => Text != null;

        public Node(string text)
        {
            Text = text;
            UpdateProperties();
        }

        public void UpdateProperties()
        {
            if (IsLeaf)
            {
                Length = Text.Length;
                LineCount = Text.Count(c => c == '\n') + 1;
                CalculateLinePositions();
            }
            else
            {
                Length = (Left?.Length ?? 0) + (Right?.Length ?? 0);
                LineCount = (Left?.LineCount ?? 0) + (Right?.LineCount ?? 0);
                MergeLinePositions();
            }
        }

        private void CalculateLinePositions()
        {
            LinePositions.Clear();
            LinePositions.Add(0);
            for (var i = 0; i < Text.Length; i++)
                if (Text[i] == '\n' && i < Text.Length - 1)
                    LinePositions.Add(i + 1);
        }

        private void MergeLinePositions()
        {
            LinePositions.Clear();
            LinePositions.AddRange(Left?.LinePositions ?? Enumerable.Empty<int>());
            var leftLength = Left?.Length ?? 0;
            if (Right != null)
                foreach (var position in Right.LinePositions)
                    LinePositions.Add(leftLength + position);
        }

        public int GetLineIndexFromPosition(int position)
        {
            if (IsLeaf)
            {
                var index = LinePositions.BinarySearch(position);
                return index < 0 ? ~index - 1 : index;
            }

            if (position < Left.Length)
                return Left.GetLineIndexFromPosition(position);
            return Left.LineCount + Right.GetLineIndexFromPosition(position - Left.Length);
        }

        public int GetLineStartPosition(int lineIndex)
        {
            if (IsLeaf) return lineIndex < LinePositions.Count ? LinePositions[lineIndex] : Length;

            if (lineIndex < Left.LineCount)
                return Left.GetLineStartPosition(lineIndex);
            return Left.Length + Right.GetLineStartPosition(lineIndex - Left.LineCount);
        }

        public void GetText(int start, int length, StringBuilder sb)
        {
            if (length == 0) return;

            if (IsLeaf)
            {
                sb.Append(Text, start, Math.Min(length, Text.Length - start));
                return;
            }

            if (start < Left.Length)
            {
                var leftLength = Math.Min(length, Left.Length - start);
                Left.GetText(start, leftLength, sb);
                length -= leftLength;
                start = 0;
            }
            else
            {
                start -= Left.Length;
            }

            if (length > 0)
                Right.GetText(start, length, sb);
        }

        public int IndexOf(char value, int startIndex)
        {
            if (IsLeaf)
            {
                var index = Text.IndexOf(value, startIndex);
                return index >= 0 ? index : -1;
            }

            if (startIndex < Left.Length)
            {
                var index = Left.IndexOf(value, startIndex);
                if (index >= 0) return index;
                startIndex = 0;
            }
            else
            {
                startIndex -= Left.Length;
            }

            var rightIndex = Right.IndexOf(value, startIndex);
            return rightIndex >= 0 ? Left.Length + rightIndex : -1;
        }

        public int GetLongestLine(out int longestLineIndex)
        {
            if (IsLeaf)
            {
                var maxLength = 0;
                longestLineIndex = 0;
                for (var i = 0; i < LinePositions.Count; i++)
                {
                    var start = LinePositions[i];
                    var end = i < LinePositions.Count - 1 ? LinePositions[i + 1] : Length;
                    var lineLength = end - start;
                    if (lineLength > maxLength)
                    {
                        maxLength = lineLength;
                        longestLineIndex = i;
                    }
                }

                return maxLength;
            }

            var leftLongest = Left.GetLongestLine(out var leftIndex);
            var rightLongest = Right.GetLongestLine(out var rightIndex);

            if (leftLongest >= rightLongest)
            {
                longestLineIndex = leftIndex;
                return leftLongest;
            }

            longestLineIndex = Left.LineCount + rightIndex;
            return rightLongest;
        }
    }
}
