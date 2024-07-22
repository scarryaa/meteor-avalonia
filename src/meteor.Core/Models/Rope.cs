using System.Text;
using meteor.Core.Interfaces;

namespace meteor.Core.Models
{
    public class Rope : IRope
    {
        private const int LEAF_MAX_LENGTH = 48;
        private Node root;

        private class Node
        {
            public string Data { get; set; }
            public Node Left { get; set; }
            public Node Right { get; set; }
            public int Length { get; set; }

            public Node(string data)
            {
                Data = data;
                Length = data.Length;
            }

            public Node(Node left, Node right)
            {
                Left = left;
                Right = right;
                Length = (left?.Length ?? 0) + (right?.Length ?? 0);
            }
        }

        public Rope(string s = "")
        {
            root = BuildTree(s);
        }

        public int Length => root?.Length ?? 0;

        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return Index(root, index);
            }
        }

        public void Insert(int index, string s)
        {
            if (index < 0 || index > Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            root = Insert(root, index, s);
        }

        public void Delete(int index, int length)
        {
            if (index < 0 || index + length > Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            root = Delete(root, index, length);
        }

        public string Substring(int startIndex, int length)
        {
            if (startIndex < 0 || startIndex + length > Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            var result = new StringBuilder(length);
            SubstringHelper(root, startIndex, length, result);
            return result.ToString();
        }

        public override string ToString()
        {
            var result = new StringBuilder(Length);
            BuildString(root, result);
            return result.ToString();
        }

        private Node BuildTree(string s)
        {
            if (string.IsNullOrEmpty(s))
                return null;
            if (s.Length <= LEAF_MAX_LENGTH)
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
                if (node.Length + s.Length <= LEAF_MAX_LENGTH)
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

            node.Length += s.Length;
            return node;
        }

        private Node Delete(Node node, int index, int length)
        {
            if (node == null || length == 0)
                return node;

            if (node.Data != null)
            {
                if (index == 0 && length >= node.Length)
                    return null;
                node.Data = node.Data.Remove(index, Math.Min(length, node.Length - index));
                node.Length = node.Data.Length;
                return node.Length > 0 ? node : null;
            }

            if (index < node.Left.Length)
            {
                node.Left = Delete(node.Left, index, length);
                if (node.Left == null)
                    return node.Right;
            }
            else
            {
                node.Right = Delete(node.Right, index - node.Left.Length, length);
                if (node.Right == null)
                    return node.Left;
            }

            node.Length = node.Left.Length + node.Right.Length;
            return node;
        }

        private void SubstringHelper(Node node, int start, int length, StringBuilder result)
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
                start = 0;
            }
            else
            {
                start -= node.Left.Length;
            }

            if (length > 0)
                SubstringHelper(node.Right, start, length, result);
        }

        public void Iterate(Action<char> action)
        {
            IterateHelper(root, action);
        }

        private void IterateHelper(Node node, Action<char> action)
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
    }
}