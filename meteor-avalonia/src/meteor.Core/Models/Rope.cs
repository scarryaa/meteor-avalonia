using System.Text;
using meteor.Core.Interfaces;

public class Rope : IRope
{
    private const int LeafMaxLength = 48;
    private ImmutableNode? _root;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    private readonly object _splitLock = new();

    private sealed class ImmutableNode
    {
        public string? Data { get; }
        public ImmutableNode? Left { get; }
        public ImmutableNode? Right { get; }
        public int Length { get; }
        public int Height { get; }

        public ImmutableNode(string? data)
        {
            Data = data;
            Length = data?.Length ?? 0;
            Height = 1;
        }

        public ImmutableNode(ImmutableNode? left, ImmutableNode? right)
        {
            Left = left;
            Right = right;
            Length = (left?.Length ?? 0) + (right?.Length ?? 0);
            Height = 1 + Math.Max(left?.Height ?? 0, right?.Height ?? 0);
        }

        public int BalanceFactor => (Left?.Height ?? 0) - (Right?.Height ?? 0);
    }

    public Rope(string? s = "")
    {
        _root = BuildTree(s);
        Length = s?.Length ?? 0;
    }

    public int Length { get; private set; }

    public char this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return IndexUnsafe(_root, index);
        }
    }

    public void Insert(int index, string? s)
    {
        if (string.IsNullOrEmpty(s)) return;

        _lock.EnterWriteLock();
        try
        {
            if (index < 0 || index > Length)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Index {index} is out of range. Valid range is 0 to {Length}.");

            _root = Insert(_root, index, s);
            Length += s.Length;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Delete(int index, int length)
    {
        if (length <= 0) return;

        _lock.EnterWriteLock();
        try
        {
            if (index < 0 || index + length > Length)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Invalid range: index={index}, length={length}. Valid range is 0 to {Length}.");

            _root = Delete(_root, index, length);
            Length -= length;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public string Substring(int startIndex, int length)
    {
        _lock.EnterReadLock();
        try
        {
            if (startIndex < 0 || startIndex > Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex),
                    $"startIndex {startIndex} is out of range. Valid range is 0 to {Length - 1}.");
            if (length < 0 || startIndex + length > Length)
                throw new ArgumentOutOfRangeException(nameof(length),
                    $"Invalid length {length}. Substring must fit within the bounds of the rope.");

            if (length == 0 || Length == 0)
                return string.Empty;

            var result = new StringBuilder(length);
            SubstringHelper(_root, startIndex, length, result);
            return result.ToString();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Iterate(Action<char> action)
    {
        _lock.EnterReadLock();
        try
        {
            IterateHelper(_root, action);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public override string ToString()
    {
        _lock.EnterReadLock();
        try
        {
            var result = new StringBuilder(Length);
            BuildString(_root, result);
            return result.ToString();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private ImmutableNode? BuildTree(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return null;
        if (s.Length <= LeafMaxLength)
            return new ImmutableNode(s);

        var mid = s.Length / 2;
        return new ImmutableNode(BuildTree(s[..mid]), BuildTree(s[mid..]));
    }

    private char IndexUnsafe(ImmutableNode node, int index)
    {
        while (true)
        {
            if (node.Data != null)
                return node.Data[index];

            var leftLength = node.Left?.Length ?? 0;
            if (index < leftLength)
            {
                node = node.Left;
            }
            else
            {
                index -= leftLength;
                node = node.Right;
            }
        }
    }

    private ImmutableNode? Insert(ImmutableNode? node, int index, string? s)
    {
        if (node == null)
            return new ImmutableNode(s);

        if (node.Data != null)
        {
            if (node.Length + s.Length <= LeafMaxLength)
                return new ImmutableNode(node.Data.Insert(index, s));

            var (left, right) = SplitStringAtomically(node.Data, index);
            return new ImmutableNode(
                new ImmutableNode(left + s),
                new ImmutableNode(right)
            );
        }

        if (index <= node.Left.Length)
            return Balance(new ImmutableNode(
                Insert(node.Left, index, s),
                node.Right
            ));
        return Balance(new ImmutableNode(
            node.Left,
            Insert(node.Right, index - node.Left.Length, s)
        ));
    }

    private ImmutableNode? Delete(ImmutableNode? node, int index, int length)
    {
        if (node == null || length == 0)
            return node;

        if (node.Data != null) return DeleteFromLeaf(node, index, length);

        return DeleteFromInternalNode(node, index, length);
    }

    private ImmutableNode? DeleteFromLeaf(ImmutableNode node, int index, int length)
    {
        if (index == 0 && length >= node.Length)
            return null;

        var newData = node.Data.Remove(index, Math.Min(length, node.Length - index));
        return newData.Length > 0 ? new ImmutableNode(newData) : null;
    }

    private ImmutableNode? DeleteFromInternalNode(ImmutableNode node, int index, int length)
    {
        if (node.Left != null && index < node.Left.Length)
        {
            var newLeft = Delete(node.Left, index, length);
            if (newLeft == null)
                return node.Right;
            return Balance(new ImmutableNode(newLeft, node.Right));
        }

        var newRight = Delete(node.Right, index - node.Left.Length, length);
        if (newRight == null)
            return node.Left;
        return Balance(new ImmutableNode(node.Left, newRight));
    }

    public override bool Equals(object? obj)
    {
        if (obj is Rope other)
            return ToString() == other.ToString();
        return false;
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    private ImmutableNode? Balance(ImmutableNode? node)
    {
        if (node == null)
            return null;

        if (node.BalanceFactor > 1)
        {
            if (node.Left != null && node.Left.BalanceFactor < 0)
                return RotateLeftRight(node);
            return RotateRight(node);
        }

        if (node.BalanceFactor < -1)
        {
            if (node.Right != null && node.Right.BalanceFactor > 0)
                return RotateRightLeft(node);
            return RotateLeft(node);
        }

        return node;
    }

    private ImmutableNode? RotateLeft(ImmutableNode? node)
    {
        var newRoot = node.Right;
        return new ImmutableNode(
            new ImmutableNode(node.Left, newRoot.Left),
            newRoot.Right
        );
    }

    private ImmutableNode? RotateRight(ImmutableNode? node)
    {
        var newRoot = node.Left;
        return new ImmutableNode(
            newRoot.Left,
            new ImmutableNode(newRoot.Right, node.Right)
        );
    }

    private ImmutableNode? RotateLeftRight(ImmutableNode? node)
    {
        return RotateRight(new ImmutableNode(
            RotateLeft(node.Left),
            node.Right
        ));
    }

    private ImmutableNode? RotateRightLeft(ImmutableNode? node)
    {
        return RotateLeft(new ImmutableNode(
            node.Left,
            RotateRight(node.Right)
        ));
    }

    private (string left, string? right) SplitStringAtomically(string? data, int index)
    {
        lock (_splitLock)
        {
            return (data[..index], data[index..]);
        }
    }

    private void SubstringHelper(ImmutableNode? node, int start, int length, StringBuilder result)
    {
        if (node == null || length <= 0)
            return;

        if (node.Data != null)
        {
            var end = Math.Min(start + length, node.Length);
            result.Append(node.Data, start, end - start);
            return;
        }

        if (start < node.Left.Length)
        {
            var leftLength = Math.Min(length, node.Left.Length - start);
            SubstringHelper(node.Left, start, leftLength, result);
            length -= leftLength;
            start = 0; // Reset start for the right node
        }
        else
        {
            start -= node.Left.Length;
        }

        if (length > 0)
            SubstringHelper(node.Right, start, length, result);
    }

    private void IterateHelper(ImmutableNode? node, Action<char> action)
    {
        if (node == null)
            return;

        if (node.Data != null)
        {
            foreach (var c in node.Data) action(c);
        }
        else
        {
            IterateHelper(node.Left, action);
            IterateHelper(node.Right, action);
        }
    }

    private void BuildString(ImmutableNode? node, StringBuilder sb)
    {
        if (node == null)
            return;

        if (node.Data != null)
        {
            sb.Append(node.Data);
        }
        else
        {
            BuildString(node.Left, sb);
            BuildString(node.Right, sb);
        }
    }
}