using System.Text;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class TextBufferService : ITextBufferService
{
    private const int ChunkSize = 4096;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;
    private int _cachedLineCount;
    private double _cachedMaxLineWidth;
    private string _cachedFontFamily;
    private double _cachedFontSize;
    private readonly StringBuilder _buffer = new();

    public TextBufferService(ITextMeasurer textMeasurer, IEditorConfig config)
    {
        _config = config;
        _textMeasurer = textMeasurer;
        _cachedLineCount = -1;
        _cachedMaxLineWidth = -1;
        _cachedFontFamily = _config.FontFamily;
        _cachedFontSize = -1;
    }

    public string GetContent()
    {
        return _buffer.ToString();
    }

    public string GetContentSliceByIndex(int startIndex, int length)
    {
        startIndex = Math.Max(0, Math.Min(startIndex, _buffer.Length));
        length = Math.Max(0, Math.Min(length, _buffer.Length - startIndex));

        if (length == 0) return string.Empty;

        return _buffer.ToString(startIndex, length);
    }

    public int GetLineIndexFromCharacterIndex(int charIndex)
    {
        if (charIndex < 0 || charIndex > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(charIndex));

        var lineCount = 0;
        for (var i = 0; i < charIndex; i++)
            if (_buffer[i] == '\n')
                lineCount++;
        return lineCount;
    }

    public int GetCharacterIndexFromLineIndex(int lineIndex)
    {
        if (lineIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        var currentLine = 0;
        for (var i = 0; i < _buffer.Length; i++)
        {
            if (currentLine == lineIndex)
                return i;
            if (_buffer[i] == '\n')
                currentLine++;
        }

        throw new ArgumentOutOfRangeException(nameof(lineIndex));
    }

    public string GetContentSlice(int startLine, int endLine)
    {
        UpdateLineCount();
        if (startLine < 0 || endLine >= _cachedLineCount || startLine > endLine)
            throw new ArgumentException("Invalid line range");

        var length = _buffer.Length;
        var currentLine = 0;
        var startPos = 0;
        var endPos = length;
        var startFound = false;

        for (var i = 0; i < length; i++)
        {
            if (currentLine == startLine && !startFound)
            {
                startPos = i;
                startFound = true;
            }

            if (currentLine == endLine + 1)
            {
                endPos = i;
                return _buffer.ToString(startPos, endPos - startPos);
            }

            if (_buffer[i] == '\n')
            {
                currentLine++;
                if (currentLine > endLine)
                {
                    endPos = i + 1; // Include the newline character
                    return _buffer.ToString(startPos, endPos - startPos);
                }
            }
        }

        return _buffer.ToString(startPos, endPos - startPos);
    }

    public void InsertText(int position, string text)
    {
        _buffer.Insert(position, text);
        InvalidateCache();
        ForceRecalculateMaxLineWidth();
    }

    public void DeleteText(int position, int length)
    {
        _buffer.Remove(position, length);
        InvalidateCache();
        ForceRecalculateMaxLineWidth();
    }

    public int GetLength()
    {
        return _buffer.Length;
    }

    public int GetLineCount()
    {
        UpdateLineCount();
        return _cachedLineCount;
    }

    public double GetMaxLineWidth(string fontFamily, double fontSize)
    {
        var recalculate = _cachedMaxLineWidth <= 0 ||
                          fontFamily != _cachedFontFamily ||
                          Math.Abs(fontSize - _cachedFontSize) > 0.001;

        if (recalculate)
        {
            _cachedMaxLineWidth = CalculateMaxLineWidth(fontFamily, fontSize);
        }

        return _cachedMaxLineWidth;
    }

    private double CalculateMaxLineWidth(string fontFamily, double fontSize)
    {
        double maxWidth = 0;
        var currentLine = new StringBuilder();

        for (var i = 0; i < _buffer.Length; i++)
        {
            var c = _buffer[i];
            if (c == '\n' || i == _buffer.Length - 1)
            {
                if (i == _buffer.Length - 1 && c != '\n')
                    currentLine.Append(c);

                var lineWidth = _textMeasurer.MeasureText(currentLine.ToString(), fontFamily, fontSize).Width;
                maxWidth = Math.Max(maxWidth, lineWidth);
                currentLine.Clear();
            }
            else
            {
                currentLine.Append(c);
            }
        }

        _cachedFontFamily = fontFamily;
        _cachedFontSize = fontSize;
        
        return maxWidth;
    }

    public string GetEntireContent()
    {
        return _buffer.ToString();
    }

    private void UpdateLineCount()
    {
        if (_cachedLineCount == -1)
        {
            _cachedLineCount = 1 + _buffer.ToString().Count(c => c == '\n');
        }
    }

    private void InvalidateCache()
    {
        _cachedLineCount = -1;
        _cachedMaxLineWidth = -1;
    }

    private void ForceRecalculateMaxLineWidth()
    {
        if (!string.IsNullOrEmpty(_cachedFontFamily) && _cachedFontSize > 0)
            _cachedMaxLineWidth = CalculateMaxLineWidth(_cachedFontFamily, _cachedFontSize);
    }
}