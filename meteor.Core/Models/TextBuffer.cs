using meteor.Core.Interfaces;
using meteor.Core.Models.Events;
using Microsoft.Extensions.Logging;

namespace meteor.Core.Models;

public class TextBuffer : ITextBuffer
{
    private IRope _rope;
    private readonly List<int> _cachedLineStarts = new() { 0 };
    private bool _isLineStartsCacheValid = true;
    private readonly ILogger<Rope> _logger;

    public TextBuffer(IRope rope, ILogger<Rope> logger)
    {
        _rope = rope ?? throw new ArgumentNullException(nameof(rope));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        UpdateLineStartsCache();
    }

    public int Length => _rope.Length;
    public int LineCount => Math.Max(1, _rope.LineCount);

    public event EventHandler<TextChangedEventArgs> TextChanged;

    public List<int> GetLineStarts()
    {
        if (!_isLineStartsCacheValid) UpdateLineStartsCache();
        return new List<int>(_cachedLineStarts);
    }

    public string Text
    {
        get => _rope.GetText() ?? "";
        set
        {
            var oldLength = Length;
            _rope = new Rope(value ?? "", _logger);
            InvalidateCache();
            OnTextChanged(0, value ?? "", oldLength);
        }
    }

    public string GetText(int start, int length)
    {
        return _rope.GetText(start, length);
    }

    public void InsertText(int position, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        _logger.LogDebug($"Inserting text '{text}' at position {position}");

        _rope.Insert(position, text);
        InvalidateCache();
        OnTextChanged(position, text, 0);

        _logger.LogDebug($"After insertion, Text is now: '{Text}', Length: {Length}, LineCount: {LineCount}");
    }

    public void DeleteText(int start, int length)
    {
        if (length <= 0 || start < 0 || start >= Length)
            return;

        _rope.Delete(start, length);
        InvalidateCache();
        OnTextChanged(start, string.Empty, length);
    }

    public void SetText(string newText)
    {
        var oldLength = Length;
        _rope = new Rope(newText, _logger);
        InvalidateCache();
        OnTextChanged(0, newText, oldLength);
    }

    public void Clear()
    {
        var oldLength = Length;
        _rope = new Rope(string.Empty, _logger);
        InvalidateCache();
        OnTextChanged(0, string.Empty, oldLength);
    }

    public string GetLineText(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
        {
            _logger.LogWarning($"Attempted to get text for invalid line index: {lineIndex}. Returning empty string.");
            return string.Empty;
        }

        var lineText = _rope.GetLineText(lineIndex);
        return lineText.TrimEnd('\r', '\n');
    }

    public int GetLineText(int lineIndex, char[] buffer)
    {
        var lineText = _rope.GetLineText(lineIndex);
        var length = Math.Min(lineText.Length, buffer.Length);
        lineText.CopyTo(0, buffer, 0, length);
        return length;
    }

    public int GetLineText(int lineIndex, Span<char> destination)
    {
        var lineText = _rope.GetLineText(lineIndex);
        var length = Math.Min(lineText.Length, destination.Length);
        lineText.AsSpan(0, length).CopyTo(destination);
        return length;
    }

    public int GetLineStartPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount)
        {
            _logger.LogError(
                $"Invalid line index {lineIndex} for GetLineStartPosition. Valid range is 0 to {LineCount - 1}.");
            throw new ArgumentOutOfRangeException(nameof(lineIndex),
                $"Line index must be between 0 and {LineCount - 1}");
        }

        return _rope.GetLineStartPosition(lineIndex);
    }

    public int GetLineEndPosition(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= LineCount) throw new ArgumentOutOfRangeException(nameof(lineIndex));

        if (lineIndex == LineCount - 1) return Length;

        return _rope.GetLineEndPosition(lineIndex);
    }


    public int GetLineLength(int lineIndex)
    {
        return _rope.GetLineLength(lineIndex);
    }

    public int GetLineIndexFromPosition(int position)
    {
        if (position < 0 || position > Length)
        {
            _logger.LogError(
                $"Invalid position {position} for GetLineIndexFromPosition. Valid range is 0 to {Length}.");
            throw new ArgumentOutOfRangeException(nameof(position), $"Position must be between 0 and {Length}");
        }

        if (position == Length) return LineCount - 1;

        var lineIndex = _rope.GetLineIndexFromPosition(position);
        return Math.Max(0, lineIndex); // Ensure non-negative
    }


    private void UpdateLineStartsCache()
    {
        _cachedLineStarts.Clear();
        _cachedLineStarts.Add(0);

        for (var i = 0; i < Math.Max(1, _rope.LineCount) - 1; i++)
        {
            _cachedLineStarts.Add(_rope.GetLineEndPosition(i) + 1);
        }

        _isLineStartsCacheValid = true;
    }

    private void InvalidateCache()
    {
        _isLineStartsCacheValid = false;
    }

    protected virtual void OnTextChanged(int position, string insertedText, int deletedLength)
    {
        TextChanged?.Invoke(this, new TextChangedEventArgs(position, insertedText, deletedLength));
    }
}
