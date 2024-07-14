using System.Text;

namespace meteor.core.Utils;

public class Rope
{
    private const int SplitThreshold = 1000;

    private readonly string _leaf;
    private Rope _left;
    private Rope _right;

    public Rope(string s)
    {
        if (s.Length <= SplitThreshold)
        {
            _leaf = s;
            Length = s.Length;
        }
        else
        {
            var mid = s.Length / 2;
            _left = new Rope(s.Substring(0, mid));
            _right = new Rope(s.Substring(mid));
            Length = _left.Length + _right.Length;
        }
    }

    public int Length { get; private set; }

    public char this[int index]
    {
        get
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException();

            if (_leaf != null)
                return _leaf[index];

            if (index < _left.Length)
                return _left[index];

            return _right[index - _left.Length];
        }
    }

    public Rope Insert(int index, string s)
    {
        if (index < 0 || index > Length)
            throw new IndexOutOfRangeException();

        if (_leaf != null)
        {
            var newString = _leaf.Insert(index, s);
            return new Rope(newString);
        }

        if (index < _left.Length)
        {
            var newLeft = _left.Insert(index, s);
            return Concat(newLeft, _right);
        }

        var newRight = _right.Insert(index - _left.Length, s);
        return Concat(_left, newRight);
    }

    public Rope Delete(int start, int length)
    {
        if (start < 0 || start + length > Length)
            throw new IndexOutOfRangeException();

        if (_leaf != null)
        {
            var newString = _leaf.Remove(start, length);
            return new Rope(newString);
        }

        if (start + length <= _left.Length)
        {
            var newLeft = _left.Delete(start, length);
            return Concat(newLeft, _right);
        }

        if (start >= _left.Length)
        {
            var newRight = _right.Delete(start - _left.Length, length);
            return Concat(_left, newRight);
        }

        var leftLength = _left.Length - start;
        var newLeftPart = _left.Delete(start, leftLength);
        var newRightPart = _right.Delete(0, length - leftLength);
        return Concat(newLeftPart, newRightPart);
    }

    public string Substring(int start, int length)
    {
        if (start < 0 || start + length > Length)
            throw new IndexOutOfRangeException();

        if (_leaf != null)
            return _leaf.Substring(start, length);

        var sb = new StringBuilder(length);
        SubstringHelper(start, length, sb);
        return sb.ToString();
    }

    private void SubstringHelper(int start, int length, StringBuilder sb)
    {
        if (_leaf != null)
        {
            sb.Append(_leaf.Substring(start, length));
            return;
        }

        if (start < _left.Length)
        {
            var leftLength = Math.Min(length, _left.Length - start);
            _left.SubstringHelper(start, leftLength, sb);
            length -= leftLength;
            start = 0;
        }
        else
        {
            start -= _left.Length;
        }

        if (length > 0)
            _right.SubstringHelper(start, length, sb);
    }

    public override string ToString()
    {
        if (_leaf != null)
            return _leaf;

        return _left + _right.ToString();
    }

    private static Rope Concat(Rope left, Rope right)
    {
        var result = new Rope(string.Empty);
        result._left = left;
        result._right = right;
        result.Length = left.Length + right.Length;
        return result;
    }
}