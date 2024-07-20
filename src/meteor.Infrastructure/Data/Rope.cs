using System.Text;
using meteor.Core.Interfaces;
using meteor.Core.Models;

namespace meteor.Infrastructure.Data;

public class Rope : IRope
{
    private const int ChunkSize = 4096;
    private const int RebalanceThreshold = 4;
    private readonly LruCache<int, string> _cache = new(100);
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    private int _insertionCount;
    private Node _root;

    public Rope() : this(string.Empty)
    {
    }

    public Rope(string s)
    {
        _root = BuildTree(s);
    }

    public int Length
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _root?.Length ?? 0;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public char this[int index]
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                if (index < 0 || index >= (_root?.Length ?? 0))
                    throw new ArgumentOutOfRangeException("index");

                return Index(_root, index);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public IRope Insert(int index, string s)
    {
        if (string.IsNullOrEmpty(s))
            return this;

        _lock.EnterWriteLock();
        try
        {
            _insertionCount++;
            var currentLength = _root?.Length ?? 0;
            if (index < 0 || index > currentLength)
                throw new ArgumentOutOfRangeException("index");

            _root = Insert(_root, index, s);
            Console.WriteLine($"Insertion {_insertionCount}: Root length after insertion = {_root.Length}");

            if (GetDepth(_root) > RebalanceThreshold * Math.Log(_root.Length, 2))
            {
                Console.WriteLine($"Rebalancing tree after insertion {_insertionCount}");
                var oldLength = _root.Length;
                _root = Rebalance(_root);
                Console.WriteLine(
                    $"Insertion {_insertionCount}: Root length after rebalance = {_root.Length}. Old length was {oldLength}");
                if (_root.Length != oldLength)
                    throw new InvalidOperationException(
                        $"Length mismatch after rebalancing. Expected {oldLength}, got {_root.Length}");
            }

            return this;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IRope Delete(int index, int length)
    {
        _lock.EnterWriteLock();
        try
        {
            if (index < 0 || index + length > GetLengthInternal())
                throw new ArgumentOutOfRangeException("index");

            if (length == 0)
                return this;

            var newRoot = Delete(_root, index, length);
            if (newRoot != null && GetDepth(newRoot) > RebalanceThreshold * Math.Log(GetLengthInternal(), 2))
                newRoot = Rebalance(newRoot);

            _root = newRoot;
            return this;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IRope GetSubstring(int start, int length)
    {
        _lock.EnterReadLock();
        try
        {
            if (start < 0 || start + length > GetLengthInternal())
                throw new ArgumentOutOfRangeException(nameof(start),
                    "Start and length were outside the bounds of the rope.");

            var result = new StringBuilder(length);
            SubstringHelper(_root, start, length, result);
            return new Rope(result.ToString());
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public string Substring(int startIndex, int length)
    {
        _lock.EnterReadLock();
        try
        {
            var cacheKey = HashCode.Combine(startIndex, length);
            if (_cache.TryGet(cacheKey, out var cachedResult)) return cachedResult;

            var result = new StringBuilder(length);
            SubstringHelper(_root, startIndex, length, result);
            var substring = result.ToString();
            _cache.Add(cacheKey, substring);
            return substring;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public override string ToString()
    {
        return new LazyString(this).ToString();
    }

    public IRope Concat(IRope other)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!(other is Rope otherRope))
                throw new ArgumentException("Can only concatenate with another Rope");

            var newRoot = new Node(_root, otherRope._root);
            if (GetDepth(newRoot) > RebalanceThreshold * Math.Log(GetLengthInternal() + otherRope.Length, 2))
                newRoot = Rebalance(newRoot);

            _root = newRoot;
            return this;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Iterate(Action<char> action)
    {
        _lock.EnterReadLock();
        try
        {
            Iterate(_root, action);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private Node BuildTree(string s)
    {
        if (string.IsNullOrEmpty(s))
            return null;
        if (s.Length <= ChunkSize)
            return new Node(s);

        var mid = s.Length / 2;
        return new Node(BuildTree(s.Substring(0, mid)), BuildTree(s.Substring(mid)));
    }

    private char Index(Node node, int index)
    {
        while (node != null)
        {
            if (node.Data != null)
                return node.Data[index];

            if (index < node.Left.Length)
            {
                node = node.Left;
            }
            else
            {
                index -= node.Left.Length;
                node = node.Right;
            }
        }

        throw new InvalidOperationException("Unexpected null node encountered.");
    }

    private Node Insert(Node node, int index, string s)
    {
        if (node == null)
            return new Node(s);

        if (node.Data != null)
        {
            if (index == 0)
                return new Node(s + node.Data);
            if (index >= node.Data.Length)
                return new Node(node.Data + s);

            if (node.Data.Length + s.Length <= ChunkSize)
            {
                node.Data = node.Data.Insert(index, s);
                node.Length = node.Data.Length;
                return node;
            }

            var left = node.Data.Substring(0, index) + s;
            var right = node.Data.Substring(index);
            return new Node(new Node(left), new Node(right));
        }

        if (index <= node.Left.Length)
            node.Left = Insert(node.Left, index, s);
        else
            node.Right = Insert(node.Right, index - node.Left.Length, s);

        node.UpdateLength();
        return node;
    }

    private Node Delete(Node node, int index, int length)
    {
        if (node == null)
            return null;

        if (node.Data != null)
        {
            if (index == 0 && length >= node.Length)
                return null;
            var newData = node.Data.Remove(index, Math.Min(length, node.Length - index));
            return string.IsNullOrEmpty(newData) ? null : new Node(newData);
        }

        Node newLeft = node.Left, newRight = node.Right;
        if (index < node.Left?.Length)
        {
            newLeft = Delete(node.Left, index, length);
            length -= Math.Min(length, node.Left?.Length ?? 0);
            index = 0;
        }
        else
        {
            index -= node.Left?.Length ?? 0;
        }

        if (length > 0 && node.Right != null)
            newRight = Delete(node.Right, index, length);

        if (newLeft == null)
            return newRight;
        if (newRight == null)
            return newLeft;
        return new Node(newLeft, newRight);
    }

    private void SubstringHelper(Node node, int start, int length, StringBuilder result)
    {
        if (node == null || length <= 0)
            return;

        if (node.Data != null)
        {
            var end = Math.Min(start + length, node.Length);
            result.Append(node.Data.Substring(start, end - start));
            return;
        }

        if (start < node.Left?.Length)
        {
            var leftLength = Math.Min(length, node.Left.Length - start);
            SubstringHelper(node.Left, start, leftLength, result);
            length -= leftLength;
            start = 0;
        }
        else
        {
            start -= node.Left?.Length ?? 0;
        }

        if (length > 0)
            SubstringHelper(node.Right, start, length, result);
    }

    private Node Rebalance(Node root)
    {
        var nodes = new List<Node>();
        FlattenTree(root, nodes);
        return BuildBalancedTree(nodes, 0, nodes.Count - 1);
    }

    private void FlattenTree(Node node, List<Node> nodes)
    {
        if (node == null)
            return;

        if (node.Data != null)
        {
            nodes.Add(node);
        }
        else
        {
            FlattenTree(node.Left, nodes);
            FlattenTree(node.Right, nodes);
        }
    }

    private Node BuildBalancedTree(List<Node> nodes, int start, int end)
    {
        if (start > end)
            return null;

        var mid = (start + end) / 2;
        var node = new Node(nodes[mid].Data);

        if (start == end)
            return node;

        node.Left = BuildBalancedTree(nodes, start, mid - 1);
        node.Right = BuildBalancedTree(nodes, mid + 1, end);
        node.UpdateLength();

        return node;
    }

    private int GetDepth(Node node)
    {
        if (node == null || node.Data != null)
            return 0;
        return 1 + Math.Max(GetDepth(node.Left), GetDepth(node.Right));
    }

    private void BuildString(Node node, StringBuilder sb)
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

    private void Iterate(Node node, Action<char> action)
    {
        if (node == null)
            return;

        if (node.Data != null)
        {
            foreach (var c in node.Data)
                action(c);
            return;
        }

        Iterate(node.Left, action);
        Iterate(node.Right, action);
    }

    private int GetLengthInternal()
    {
        return _root?.Length ?? 0;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var other = (Rope)obj;
        return ToString().Equals(other.ToString());
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    private sealed class Node
    {
        public string Data;
        public Node Left;
        public int Length;
        public Node Right;

        public Node()
        {
        }

        public Node(string data)
        {
            Data = data;
            Length = data.Length;
        }

        public Node(Node left, Node right)
        {
            Left = left;
            Right = right;
            UpdateLength();
        }

        public void UpdateLength()
        {
            Length = (Left?.Length ?? 0) + (Right?.Length ?? 0);
            if (Data != null) Length += Data.Length;
        }
    }

    private sealed class LazyString
    {
        private readonly Rope _rope;
        private string _cachedString;

        public LazyString(Rope rope)
        {
            _rope = rope;
        }

        public override string ToString()
        {
            if (_cachedString == null)
            {
                var sb = new StringBuilder(_rope._root?.Length ?? 0);
                _rope.BuildString(_rope._root, sb);
                _cachedString = sb.ToString();
            }

            return _cachedString;
        }
    }
}