using System.Text;
using meteor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace meteor.Core.Models;

public class Rope : IRope
{
    private const int ChunkSize = 4096;
    private Node _root;
    private readonly List<List<int>> _cachedLineStarts = new();
    private bool _isLineStartsCacheValid;
    private const int LineCacheChunkSize = 1000;
    private const int RebalanceThreshold = 2;

    private readonly ILogger _logger;

    public Rope(string text, ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogDebug("Initializing Rope with text");
        _root = BuildBalanced(text ?? "");
        Length = text?.Length ?? 0;
        LineCount = Math.Max(1, text?.Split('\n').Length ?? 1);
        UpdateLineStartsCache();
    }

    public int Length { get; set; }

    public int LineCount { get; private set; }

    private Node Build(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new Node(new char[0]);

        var chunks = new List<Node>();
        for (var i = 0; i < text.Length; i += ChunkSize)
        {
            var length = Math.Min(ChunkSize, text.Length - i);
            var chunk = new char[length];
            text.CopyTo(i, chunk, 0, length);
            chunks.Add(new Node(chunk));
        }

        while (chunks.Count > 1)
        {
            var newChunks = new List<Node>();
            for (var i = 0; i < chunks.Count; i += 2)
                if (i + 1 < chunks.Count)
                    newChunks.Add(new Node(chunks[i], chunks[i + 1]));
                else
                    newChunks.Add(chunks[i]);
            chunks = newChunks;
        }

        return chunks[0];
    }

    private Node BuildBalanced(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new Node(new char[0]);

        var chunks = new List<Node>();
        for (var i = 0; i < text.Length; i += ChunkSize)
        {
            var length = Math.Min(ChunkSize, text.Length - i);
            var chunk = new char[length];
            text.CopyTo(i, chunk, 0, length);
            chunks.Add(new Node(chunk));
        }

        return BuildBalancedTree(chunks, 0, chunks.Count - 1);
    }

    public void Insert(int index, string text)
    {
        _logger.LogDebug($"Inserting text at index {index}");
        if (string.IsNullOrEmpty(text)) return;
        if (index > Length) index = Length;

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
        {
            _root = Insert(_root, index, lines[i]);
            index += lines[i].Length;

            if (i < lines.Length - 1)
            {
                var lineEnding = DetermineLineEnding(text, lines[i]);
                _root = Insert(_root, index, lineEnding);
                index += lineEnding.Length;
                LineCount++;
            }
        }

        Length += text.Length;
        InvalidateCache();
        UpdateLineStartsCache();

        _logger.LogDebug($"After insertion, rope length: {Length}, LineCount: {LineCount}");
    }

    private string DetermineLineEnding(string text, string line)
    {
        var index = text.IndexOf(line, StringComparison.Ordinal) + line.Length;
        if (index < text.Length)
        {
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                return "\r\n";
            if (text[index] == '\r')
                return "\r";
            if (text[index] == '\n')
                return "\n";
        }

        return "\n"; // Default to '\n' if we can't determine the line ending
    }
    
    private Node Insert(Node node, int index, string text)
    {
        if (node == null) return new Node(text.ToCharArray());

        if (node.IsLeaf)
        {
            if (index < 0 || index > node.Length) return node;

            var newContent = new char[node.Length + text.Length];
            Array.Copy(node.Chunk, 0, newContent, 0, index);
            text.CopyTo(0, newContent, index, text.Length);
            Array.Copy(node.Chunk, index, newContent, index + text.Length, node.Length - index);
            return new Node(newContent);
        }

        if (index <= node.Left.Length)
            node.Left = Insert(node.Left, index, text);
        else
            node.Right = Insert(node.Right, index - node.Left.Length, text);

        node.UpdateProperties();
        return Balance(node);
    }

    private Node BuildBalancedTree(List<Node> nodes, int start, int end)
    {
        _logger.LogDebug($"Building balanced tree from {start} to {end}");
        if (start > end)
            return null;
        if (start == end)
            return nodes[start];

        var mid = (start + end) / 2;
        var left = BuildBalancedTree(nodes, start, mid);
        var right = BuildBalancedTree(nodes, mid + 1, end);
        return new Node(left, right);
    }

    private Node Balance(Node node)
    {
        if (node == null)
        {
            _logger.LogDebug("Balance: node is null, returning null");
            return null;
        }

        var leftHeight = node.Left?.Height ?? 0;
        var rightHeight = node.Right?.Height ?? 0;

        _logger.LogDebug($"Balancing node with left height {leftHeight} and right height {rightHeight}");

        if (Math.Abs(leftHeight - rightHeight) > RebalanceThreshold)
        {
            if (leftHeight > rightHeight)
            {
                _logger.LogDebug("Performing right rotation");
                return RotateRight(node);
            }

            _logger.LogDebug("Performing left rotation");
            return RotateLeft(node);
        }

        return node;
    }


    private Node RotateRight(Node node)
    {
        if (node?.Left == null)
        {
            _logger.LogDebug("RotateRight: Cannot rotate, left child is null");
            return node;
        }

        _logger.LogDebug("Rotating right");
        var newRoot = node.Left;
        node.Left = newRoot.Right;
        newRoot.Right = node;
        node.UpdateProperties();
        newRoot.UpdateProperties();
        return newRoot;
    }

    private Node RotateLeft(Node node)
    {
        if (node?.Right == null)
        {
            _logger.LogDebug("RotateLeft: Cannot rotate, right child is null");
            return node;
        }

        _logger.LogDebug("Rotating left");
        var newRoot = node.Right;
        node.Right = newRoot.Left;
        newRoot.Left = node;
        node.UpdateProperties();
        newRoot.UpdateProperties();
        return newRoot;
    }
    
    public void Delete(int start, int length)
    {
        _logger.LogDebug($"Deleting text from {start} to {start + length}");
        if (length <= 0) return;
        var end = Math.Min(start + length, Length);
        var actualLength = end - start;
        _root = Delete(_root, start, actualLength);
        _root = Balance(_root);
        Length = _root?.Length ?? 0;
        InvalidateCache();
        UpdateLineStartsCache();
        _logger.LogDebug($"After deletion, rope length: {Length}");
    }

    public string GetText()
    {
        _logger.LogDebug("Getting all text");
        return GetText(0, Length) ?? "";
    }

    public int GetLineLength(int lineIndex)
    {
        _logger.LogDebug($"Getting length of line {lineIndex}");
        if (lineIndex < 0 || lineIndex >= LineCount)
            return 0;

        UpdateLineStartsCache();
        var start = GetLineStartPosition(lineIndex);
        var end = lineIndex < LineCount - 1 ? GetLineStartPosition(lineIndex + 1) : Length;
        return end - start;
    }

    private Node Delete(Node node, int start, int length)
    {
        if (node == null) return null;

        _logger.LogDebug($"Delete: node length = {node.Length}, start = {start}, length = {length}");

        if (node.IsLeaf)
        {
            if (start >= node.Length) return node;

            var endIndex = Math.Min(start + length, node.Length);
            var newLength = node.Length - (endIndex - start);

            if (newLength == 0) return null;

            var newChunk = new char[newLength];
            Array.Copy(node.Chunk, 0, newChunk, 0, start);
            Array.Copy(node.Chunk, endIndex, newChunk, start, node.Length - endIndex);

            return new Node(newChunk);
        }

        var leftLength = node.Left?.Length ?? 0;

        if (start < leftLength)
        {
            node.Left = Delete(node.Left, start, Math.Min(length, leftLength - start));
            length -= Math.Min(length, leftLength - start);
            start = 0;
        }
        else
        {
            start -= leftLength;
        }

        if (length > 0 && node.Right != null) node.Right = Delete(node.Right, start, length);

        if (node.Left == null) return node.Right;
        if (node.Right == null) return node.Left;

        node.UpdateProperties();
        return node;
    }
    
    public string GetText(int start, int length)
    {
        _logger.LogDebug($"GetText called with start={start}, length={length}");
        if (Length == 0 || start >= Length)
        {
            _logger.LogDebug("Returning empty string due to out-of-bounds request");
            return string.Empty;
        }

        start = Math.Max(0, Math.Min(start, Length - 1));
        length = Math.Max(0, Math.Min(length, Length - start));

        _logger.LogDebug($"Adjusted parameters: start={start}, length={length}");

        if (length == 0)
        {
            _logger.LogDebug("Returning empty string due to zero length");
            return string.Empty;
        }

        var sb = new StringBuilder(length);
        GetText(_root, start, length, sb);
        var result = sb.ToString();
        _logger.LogDebug($"GetText returning: '{result}'");
        return result;
    }

    private void GetText(Node node, int start, int length, StringBuilder sb)
    {
        if (node == null || length <= 0)
        {
            _logger.LogDebug("GetText (private) early return: node is null or length <= 0");
            return;
        }

        _logger.LogDebug($"GetText (private) called with start={start}, length={length}, node.Length={node.Length}");

        if (node.IsLeaf)
        {
            var copyStart = Math.Max(0, Math.Min(start, node.Length - 1));
            var copyLength = Math.Min(length, node.Length - copyStart);
            _logger.LogDebug($"Leaf node: copyStart={copyStart}, copyLength={copyLength}");
            if (copyLength > 0)
            {
                sb.Append(node.Chunk, copyStart, copyLength);
                _logger.LogDebug($"Appended {copyLength} characters to StringBuilder");
            }
        }
        else
        {
            var leftLength = node.Left?.Length ?? 0;
            _logger.LogDebug($"Internal node: leftLength={leftLength}");
            if (start < leftLength)
            {
                var leftCopyLength = Math.Min(length, leftLength - start);
                _logger.LogDebug($"Traversing left child: start={start}, length={leftCopyLength}");
                GetText(node.Left, start, leftCopyLength, sb);
                length -= leftCopyLength;
                start = 0;
            }
            else
            {
                start -= leftLength;
            }

            if (length > 0)
            {
                _logger.LogDebug($"Traversing right child: start={start}, length={length}");
                GetText(node.Right, start, length, sb);
            }
        }
    }

    public string GetLineText(int lineIndex)
    {
        _logger.LogDebug($"Getting text for line {lineIndex}");
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex),
                $"Line index must be between 0 and {LineCount - 1}");

        var start = GetLineStartPosition(lineIndex);
        var end = lineIndex == LineCount - 1 ? Length : GetLineStartPosition(lineIndex + 1);
        var lineText = GetText(start, end - start);
        return lineText.TrimEnd('\r', '\n');
    }

    public int GetLineIndexFromPosition(int position)
    {
        _logger.LogDebug($"Getting line index from position {position}");
        if (position < 0)
            return 0;
        if (position >= Length)
            return LineCount - 1;

        UpdateLineStartsCache();

        var chunkIndex = 0;
        while (chunkIndex < _cachedLineStarts.Count)
        {
            var chunk = _cachedLineStarts[chunkIndex];
            if (chunk[chunk.Count - 1] > position)
                break;
            chunkIndex++;
        }

        if (chunkIndex == _cachedLineStarts.Count)
            return LineCount - 1;

        var targetChunk = _cachedLineStarts[chunkIndex];
        var innerIndex = targetChunk.BinarySearch(position);

        if (innerIndex < 0)
            innerIndex = ~innerIndex - 1;

        return Math.Min(chunkIndex * LineCacheChunkSize + innerIndex, LineCount - 1);
    }

    public int GetLineStartPosition(int lineIndex)
    {
        _logger.LogDebug($"Getting start position for line {lineIndex}");
        if (lineIndex < 0 || lineIndex >= LineCount)
            return 0;

        UpdateLineStartsCache();

        var chunkIndex = lineIndex / LineCacheChunkSize;
        var innerIndex = lineIndex % LineCacheChunkSize;

        if (chunkIndex >= _cachedLineStarts.Count)
            return Length;

        var chunk = _cachedLineStarts[chunkIndex];
        return innerIndex < chunk.Count ? chunk[innerIndex] : Length;
    }

    public int GetLineEndPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount) throw new ArgumentOutOfRangeException(nameof(lineIndex));

        if (lineIndex == LineCount - 1)
            // For the last line, return the length of the rope
            return Length;

        // For other lines, find the position of the newline character
        return IndexOf('\n', GetLineStartPosition(lineIndex));
    }


    private void InvalidateCache()
    {
        _logger.LogDebug("Invalidating cache");
        _isLineStartsCacheValid = false;
    }

    private void UpdateLineStartsCache()
    {
        _logger.LogDebug("Updating line starts cache");
        if (_isLineStartsCacheValid) return;

        _cachedLineStarts.Clear();
        LineCount = 1; // Start with 1 to account for the first line

        var currentChunk = new List<int>(LineCacheChunkSize) { 0 };
        _cachedLineStarts.Add(currentChunk);

        var position = 0;
        while (position < Length)
        {
            var newLineIndex = IndexOf('\n', position);
            if (newLineIndex == -1)
                // No more newlines found, exit the loop
                break;

            position = newLineIndex + 1;
            LineCount++;

            if (LineCount % LineCacheChunkSize == 1)
            {
                // Start a new chunk when we've filled the previous one
                currentChunk = new List<int>(LineCacheChunkSize);
                _cachedLineStarts.Add(currentChunk);
            }

            currentChunk.Add(position);
        }

        _isLineStartsCacheValid = true;
    }

    public int IndexOf(char value, int startIndex)
    {
        _logger.LogDebug($"Finding index of '{value}' starting from {startIndex}");
        if (startIndex < 0 || startIndex >= Length)
            return -1;

        return IndexOf(_root, value, startIndex);
    }

    private int IndexOf(Node node, char value, int startIndex)
    {
        if (node == null)
            return -1;

        if (node.IsLeaf)
        {
            for (var i = Math.Max(0, startIndex); i < node.Chunk.Length; i++)
                if (node.Chunk[i] == value)
                {
                    _logger.LogDebug($"Found '{value}' at position {i}");
                    return i;
                }

            return -1;
        }

        if (startIndex < node.Left.Length)
        {
            var leftResult = IndexOf(node.Left, value, startIndex);
            if (leftResult != -1)
                return leftResult;

            var rightResult = IndexOf(node.Right, value, 0);
            return rightResult != -1 ? node.Left.Length + rightResult : -1;
        }
        else
        {
            var rightResult = IndexOf(node.Right, value, startIndex - node.Left.Length);
            return rightResult != -1 ? node.Left.Length + rightResult : -1;
        }
    }

    private class Node
    {
        public char[] Chunk { get; }
        public Node Left { get; set; }
        public Node Right { get; set; }
        public int Length { get; private set; }
        public int Height { get; private set; }
        public bool IsLeaf => Chunk != null;

        public Node(char[] chunk)
        {
            Chunk = chunk;
            Length = chunk.Length;
            Height = 1;
        }

        public Node(Node left, Node right)
        {
            Left = left;
            Right = right;
            Chunk = null;
            UpdateProperties();
        }

        public void UpdateProperties()
        {
            Length = (Left?.Length ?? 0) + (Right?.Length ?? 0);
            Height = 1 + Math.Max(Left?.Height ?? 0, Right?.Height ?? 0);
        }
    }
}