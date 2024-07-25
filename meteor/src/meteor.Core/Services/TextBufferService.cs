using System.Text;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

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
        var startFound = false;

        for (var i = 0; i < length; i += ChunkSize)
        {
            var chunk = TextBuffer.GetDocumentSlice(i, Math.Min(i + ChunkSize, length));
            for (var j = 0; j < chunk.Length; j++)
            {
                if (currentLine == startLine && !startFound)
                {
                    startPos = i + j;
                    startFound = true;
                }
                if (currentLine == endLine + 1)
                {
                    endPos = i + j;
                    return TextBuffer.GetDocumentSlice(startPos, endPos);
                }

                if (chunk[j] == '\n')
                {
                    currentLine++;
                    if (currentLine > endLine)
                    {
                        endPos = i + j + 1; // Include the newline character
                        return TextBuffer.GetDocumentSlice(startPos, endPos);
                    }
                }
            }
        }

        return TextBuffer.GetDocumentSlice(startPos, endPos);
    }

    public void InsertText(int position, string text)
    {
        TextBuffer.InsertText(position, text);
        InvalidateCache();
        ForceRecalculateMaxLineWidth();
    }

    public void DeleteText(int position, int length)
    {
        TextBuffer.DeleteText(position, length);
        InvalidateCache();
        ForceRecalculateMaxLineWidth();
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
        var length = GetLength();
        var currentLineLength = 0;
        var longestLine = new StringBuilder();
        var currentLine = new StringBuilder();

        for (var i = 0; i < length; i += ChunkSize)
        {
            var chunk = TextBuffer.GetDocumentSlice(i, Math.Min(i + ChunkSize, length));
            foreach (var c in chunk)
                if (c == '\n')
                {
                    if (currentLine.Length > longestLine.Length)
                    {
                        longestLine.Clear();
                        longestLine.Append(currentLine.ToString());
                    }
                    currentLine.Clear();
                }
                else
                {
                    currentLine.Append(c);
                }
        }

        // Always check the last line, even if it doesn't end with a newline
        if (currentLine.Length > longestLine.Length)
        {
            longestLine.Clear();
            longestLine.Append(currentLine.ToString());
        }

        // Measure the longest line
        if (longestLine.Length > 0)
        {
            maxWidth = _textMeasurer.MeasureText(longestLine.ToString(), fontFamily, fontSize).Width;
        }
        
        return maxWidth;
    }

    private double UpdateMaxWidth(StringBuilder line, string fontFamily, double fontSize, double currentMaxWidth)
    {
        if (line.Length > 0)
        {
            var lineWidth = MeasureLineWidth(line, fontFamily, fontSize);
            var newMaxWidth = Math.Max(currentMaxWidth, lineWidth);
            return newMaxWidth;
        }

        return currentMaxWidth;
    }

    public string GetEntireContent()
    {
        return TextBuffer.GetDocumentSlice(0, TextBuffer.GetDocumentLength());
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

    private void ForceRecalculateMaxLineWidth()
    {
        if (!string.IsNullOrEmpty(_cachedFontFamily) && _cachedFontSize > 0)
            _cachedMaxLineWidth = CalculateMaxLineWidth(_cachedFontFamily, _cachedFontSize);
    }
}
