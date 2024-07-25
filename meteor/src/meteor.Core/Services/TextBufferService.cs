using System.Text;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class TextBufferService : ITextBufferService
{
    private const int ChunkSize = 4096;
    private readonly ITextMeasurer _textMeasurer;
    private int _cachedLineCount;
    private double _cachedMaxLineWidth;
    private string _cachedFontFamily;
    private double _cachedFontSize;

    public TextBufferService(ITextMeasurer textMeasurer)
    {
        _textMeasurer = textMeasurer;
        _cachedLineCount = -1;
        _cachedMaxLineWidth = -1;
        _cachedFontFamily = string.Empty;
        _cachedFontSize = -1;
    }

    public string GetContent()
    {
        return TextBuffer.GetDocumentSlice(0, TextBuffer.GetDocumentLength());
    }

    public string GetContentSlice(int startLine, int endLine)
    {
        UpdateLineCount();
        if (startLine < 0 || endLine >= _cachedLineCount || startLine > endLine)
            throw new ArgumentException("Invalid line range");

        var length = TextBuffer.GetDocumentLength();
        var currentLine = 0;
        var startPos = 0;
        var endPos = length;

        for (var i = 0; i < length; i += ChunkSize)
        {
            var chunk = TextBuffer.GetDocumentSlice(i, Math.Min(i + ChunkSize, length));
            for (var j = 0; j < chunk.Length; j++)
            {
                if (currentLine == startLine && startPos == 0)
                    startPos = i + j;
                if (currentLine == endLine + 1)
                {
                    endPos = i + j;
                    return TextBuffer.GetDocumentSlice(startPos, endPos);
                }

                if (chunk[j] == '\n')
                    currentLine++;
            }
        }

        return TextBuffer.GetDocumentSlice(startPos, endPos);
    }

    public void InsertText(int position, string text)
    {
        TextBuffer.InsertText(position, text);
        InvalidateCache();
    }

    public void DeleteText(int position, int length)
    {
        TextBuffer.DeleteText(position, length);
        InvalidateCache();
    }

    public int GetLength()
    {
        return TextBuffer.GetDocumentLength();
    }

    public int GetLineCount()
    {
        UpdateLineCount();
        return _cachedLineCount;
    }

    public double GetMaxLineWidth(string fontFamily, double fontSize)
    {
        // Always recalculate if the cache is invalid or font settings have changed
        if (_cachedMaxLineWidth <= 0 || fontFamily != _cachedFontFamily || Math.Abs(fontSize - _cachedFontSize) > 0.001)
        {
            _cachedMaxLineWidth = CalculateMaxLineWidth(fontFamily, fontSize);
            _cachedFontFamily = fontFamily;
            _cachedFontSize = fontSize;
        }

        return _cachedMaxLineWidth;
    }

    private double CalculateMaxLineWidth(string fontFamily, double fontSize)
    {
        double maxWidth = 0;
        var length = GetLength();
        var currentLine = new StringBuilder();

        for (var i = 0; i < length; i += ChunkSize)
        {
            var chunk = TextBuffer.GetDocumentSlice(i, Math.Min(i + ChunkSize, length));
            foreach (var c in chunk)
                if (c == '\n')
                {
                    var lineWidth = MeasureLineWidth(currentLine, fontFamily, fontSize);
                    maxWidth = Math.Max(maxWidth, lineWidth);
                    currentLine.Clear();
                }
                else
                {
                    currentLine.Append(c);
                }
        }

        // Check the last line
        if (currentLine.Length > 0)
        {
            var lineWidth = MeasureLineWidth(currentLine, fontFamily, fontSize);
            maxWidth = Math.Max(maxWidth, lineWidth);
        }

        return maxWidth;
    }

    private double MeasureLineWidth(StringBuilder line, string fontFamily, double fontSize)
    {
        var width = _textMeasurer.MeasureText(line.ToString(), fontFamily, fontSize).Width;
        return width;
    }

    private void UpdateLineCount()
    {
        var lineCount = 1;
        var length = TextBuffer.GetDocumentLength();

        for (var i = 0; i < length; i += ChunkSize)
        {
            var chunk = TextBuffer.GetDocumentSlice(i, Math.Min(i + ChunkSize, length));
            lineCount += chunk.Count(c => c == '\n');
        }

        _cachedLineCount = lineCount;
    }

    private void InvalidateCache()
    {
        _cachedLineCount = -1;
        _cachedMaxLineWidth = -1;
    }
}