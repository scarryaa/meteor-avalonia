using System.Text;
using meteor.Core.Interfaces;

namespace meteor.Core.Models;

public class Rope : IRope
{
    private const int SplitLength = 1024;
    private Node _root;

    public Rope(string text)
    {
        _root = Build(text);
    }

    public int Length => _root?.Length ?? 0;

    public int LineCount => CalculateLineCount();

    private Node Build(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new Node(string.Empty);

        if (text.Length <= SplitLength)
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

    public void Insert(int index, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        index = Math.Max(0, Math.Min(index, Length));
        _root = Insert(_root, index, text);
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

        _root = Delete(_root, start, length);
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
        _root.GetText(start, length, sb);
        return sb.ToString();
    }

    public string GetLineText(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            return string.Empty;

        var start = GetLineStartPosition(lineIndex);
        var length = GetLineLength(lineIndex);
        return GetText(start, length);
    }

    public int GetLineIndexFromPosition(int position)
    {
        if (position < 0 || position > Length)
            throw new ArgumentOutOfRangeException(nameof(position));

        return _root.GetLineIndexFromPosition(position);
    }

    public int GetLineStartPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        return _root.GetLineStartPosition(lineIndex);
    }

    public int GetLineEndPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        if (lineIndex == LineCount - 1)
            return Length;

        return GetLineStartPosition(lineIndex + 1) - 1;
    }

    public int GetLineLength(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        var start = GetLineStartPosition(lineIndex);
        var end = lineIndex < LineCount - 1 ? GetLineStartPosition(lineIndex + 1) : Length;
        return end - start;
    }

    public int IndexOf(char value, int startIndex = 0)
    {
        if (startIndex < 0 || startIndex >= Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        return _root.IndexOf(value, startIndex);
    }

    private int CalculateLineCount()
    {
        return _root?.LineCount ?? 0;
    }

    private class Node
    {
        public string Text { get; set; }
        public Node Left { get; set; }
        public Node Right { get; set; }
        public int Length { get; private set; }
        public int LineCount { get; private set; }
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
            }
            else
            {
                Length = (Left?.Length ?? 0) + (Right?.Length ?? 0);
                LineCount = (Left?.LineCount ?? 0) + (Right?.LineCount ?? 0);
            }
        }

        public int GetLineIndexFromPosition(int position)
        {
            if (IsLeaf)
            {
                var lineCount = 0;
                for (var i = 0; i < Math.Min(position, Text.Length); i++)
                    if (Text[i] == '\n')
                        lineCount++;
                return lineCount;
            }

            if (position < Left.Length)
                return Left.GetLineIndexFromPosition(position);
            return Left.LineCount + Right.GetLineIndexFromPosition(position - Left.Length);
        }

        public int GetLineStartPosition(int lineIndex)
        {
            if (IsLeaf)
            {
                var currentLine = 0;
                for (var i = 0; i < Text.Length; i++)
                {
                    if (currentLine == lineIndex)
                        return i;
                    if (Text[i] == '\n')
                        currentLine++;
                }

                return Length;
            }

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
    }
}