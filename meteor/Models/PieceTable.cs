using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace meteor.Models;

public class PieceTable
{
    private readonly string _original;
    private readonly List<Piece> _pieces;
    private readonly StringBuilder _add;
    private List<int> _lineStarts;

    public PieceTable(string text)
    {
        _original = text;
        _add = new StringBuilder();
        _pieces = new List<Piece> { new(0, text.Length, BufferType.Original) };
        UpdateLineInfo();
    }

    public int Length => _pieces.Sum(p => p.Length);

    public int LineCount { get; private set; }

    public void Insert(int position, string text)
    {
        var pieceIndex = FindPieceIndex(position);
        var offset = position - GetPieceStart(pieceIndex);
        var piece = _pieces[pieceIndex];

        if (offset == 0)
        {
            _pieces.Insert(pieceIndex, new Piece(_add.Length, text.Length, BufferType.Add));
        }
        else if (offset == piece.Length)
        {
            _pieces.Insert(pieceIndex + 1, new Piece(_add.Length, text.Length, BufferType.Add));
        }
        else
        {
            _pieces[pieceIndex] = new Piece(piece.Start, offset, piece.Type);
            _pieces.Insert(pieceIndex + 1, new Piece(_add.Length, text.Length, BufferType.Add));
            _pieces.Insert(pieceIndex + 2, new Piece(piece.Start + offset, piece.Length - offset, piece.Type));
        }

        _add.Append(text);
        UpdateLineInfo();
    }

    public void Delete(int start, int length)
    {
        var startPieceIndex = FindPieceIndex(start);
        var endPieceIndex = FindPieceIndex(start + length);

        var startOffset = start - GetPieceStart(startPieceIndex);
        var endOffset = start + length - GetPieceStart(endPieceIndex);

        if (startPieceIndex == endPieceIndex)
        {
            var piece = _pieces[startPieceIndex];
            if (startOffset == 0 && endOffset == piece.Length)
            {
                _pieces.RemoveAt(startPieceIndex);
            }
            else if (startOffset == 0)
            {
                _pieces[startPieceIndex] = new Piece(piece.Start + endOffset, piece.Length - endOffset, piece.Type);
            }
            else if (endOffset == piece.Length)
            {
                _pieces[startPieceIndex] = new Piece(piece.Start, startOffset, piece.Type);
            }
            else
            {
                _pieces[startPieceIndex] = new Piece(piece.Start, startOffset, piece.Type);
                _pieces.Insert(startPieceIndex + 1,
                    new Piece(piece.Start + endOffset, piece.Length - endOffset, piece.Type));
            }
        }
        else
        {
            var startPiece = _pieces[startPieceIndex];
            var endPiece = _pieces[endPieceIndex];

            if (startOffset == 0)
            {
                _pieces.RemoveAt(startPieceIndex);
            }
            else
            {
                _pieces[startPieceIndex] = new Piece(startPiece.Start, startOffset, startPiece.Type);
                startPieceIndex++;
            }

            if (endOffset == endPiece.Length)
                _pieces.RemoveAt(endPieceIndex);
            else
                _pieces[endPieceIndex] = new Piece(endPiece.Start + endOffset, endPiece.Length - endOffset,
                    endPiece.Type);

            _pieces.RemoveRange(startPieceIndex, endPieceIndex - startPieceIndex);
        }

        UpdateLineInfo();
    }

    public string GetText(int start, int length)
    {
        var result = new StringBuilder();
        var remaining = length;
        var currentPosition = start;

        while (remaining > 0)
        {
            var pieceIndex = FindPieceIndex(currentPosition);
            var piece = _pieces[pieceIndex];
            var pieceStart = GetPieceStart(pieceIndex);
            var offset = currentPosition - pieceStart;
            var pieceRemaining = piece.Length - offset;
            var toTake = Math.Min(remaining, pieceRemaining);

            var buffer = piece.Type == BufferType.Original ? _original : _add.ToString();
            result.Append(buffer.Substring(piece.Start + offset, toTake));

            currentPosition += toTake;
            remaining -= toTake;
        }

        return result.ToString();
    }

    private int FindPieceIndex(int position)
    {
        var currentPosition = 0;
        for (var i = 0; i < _pieces.Count; i++)
        {
            if (currentPosition + _pieces[i].Length > position) return i;
            currentPosition += _pieces[i].Length;
        }

        return _pieces.Count - 1;
    }

    private int GetPieceStart(int pieceIndex)
    {
        var start = 0;
        for (var i = 0; i < pieceIndex; i++) start += _pieces[i].Length;
        return start;
    }

    private void UpdateLineInfo()
    {
        LineCount = 1;
        _lineStarts = new List<int> { 0 };

        var position = 0;
        foreach (var piece in _pieces)
        {
            var buffer = piece.Type == BufferType.Original ? _original : _add.ToString();
            for (var i = 0; i < piece.Length; i++)
                if (buffer[piece.Start + i] == '\n')
                {
                    LineCount++;
                    _lineStarts.Add(position + i + 1);
                }

            position += piece.Length;
        }
    }

    public int GetLineStartPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        return _lineStarts[lineIndex];
    }

    public int IndexOf(string value, int startIndex = 0)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (startIndex < 0 || startIndex > Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        if (value.Length == 0)
            return startIndex;

        var position = startIndex;
        var buffer = GetText(0, Length);
        var index = buffer.IndexOf(value, position, StringComparison.Ordinal);

        return index >= 0 ? index : -1;
    }

    public int GetLineEndPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        return lineIndex < LineCount - 1 ? _lineStarts[lineIndex + 1] - 1 : Length - 1;
    }

    public int GetLineIndexFromPosition(int position)
    {
        if (position < 0 || position > Length)
            throw new ArgumentOutOfRangeException(nameof(position));

        return _lineStarts.BinarySearch(position) switch
        {
            int index when index >= 0 => index,
            int index => ~index - 1
        };
    }

    private enum BufferType
    {
        Original,
        Add
    }

    private struct Piece(int start, int length, BufferType type)
    {
        public int Start { get; } = start;
        public int Length { get; } = length;
        public BufferType Type { get; } = type;
    }
}