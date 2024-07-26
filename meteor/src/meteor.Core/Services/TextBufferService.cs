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
    private double _cachedMaxLineWidth;
    private string _cachedFontFamily;
    private double _cachedFontSize;
    private readonly List<int> _lineStartIndices;
    private readonly List<int> _chunkStartIndices;

    public TextBufferService(ITextMeasurer textMeasurer, IEditorConfig config)
    {
        _config = config;
        _textMeasurer = textMeasurer;
        _cachedMaxLineWidth = -1;
        _cachedFontFamily = _config.FontFamily;
        _cachedFontSize = -1;
        _lineStartIndices = new List<int> { 0 };
        _chunkStartIndices = new List<int> { 0 };
        UpdateLineIndices();
    }

    public string GetContent()
    {
        return TextBuffer.GetDocumentSlice(0, TextBuffer.GetDocumentLength());
    }

    public string GetContentSliceByIndex(int startIndex, int length)
    {
        var documentLength = TextBuffer.GetDocumentLength();
        startIndex = Math.Clamp(startIndex, 0, documentLength);
        length = Math.Clamp(length, 0, documentLength - startIndex);
        return length == 0 ? string.Empty : TextBuffer.GetDocumentSlice(startIndex, startIndex + length);
    }

    public int GetLineIndexFromCharacterIndex(int charIndex)
    {
        return _lineStartIndices.BinarySearch(charIndex) switch
        {
            var i when i >= 0 => i,
            var i => ~i - 1
        };
    }

    public int GetCharacterIndexFromLineIndex(int lineIndex)
    {
        return lineIndex < 0 ? 0 :
            lineIndex >= _lineStartIndices.Count ? TextBuffer.GetDocumentLength() :
            _lineStartIndices[lineIndex];
    }

    public string GetContentSlice(int startLine, int endLine)
    {
        startLine = Math.Clamp(startLine, 0, _lineStartIndices.Count - 1);
        endLine = Math.Clamp(endLine, startLine, _lineStartIndices.Count - 1);

        var startIndex = _lineStartIndices[startLine];
        var endIndex = endLine < _lineStartIndices.Count - 1
            ? _lineStartIndices[endLine + 1]
            : TextBuffer.GetDocumentLength();

        return TextBuffer.GetDocumentSlice(startIndex, endIndex);
    }

    public void InsertText(int position, string text)
    {
        TextBuffer.InsertText(position, text);
        UpdateLineIndicesAfterInsert(position, text);
        InvalidateCache();
    }

    public void DeleteText(int position, int length)
    {
        TextBuffer.DeleteText(position, length);
        UpdateLineIndicesAfterDelete(position, length);
        InvalidateCache();
    }

    public int GetLength()
    {
        return TextBuffer.GetDocumentLength();
    }

    public int GetLineCount()
    {
        return _lineStartIndices.Count;
    }

    public double GetMaxLineWidth(string fontFamily, double fontSize)
    {
        fontFamily = string.Intern(fontFamily);
        if (_cachedMaxLineWidth <= 0 || !ReferenceEquals(fontFamily, _cachedFontFamily) ||
            Math.Abs(fontSize - _cachedFontSize) > 0.001)
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
        var sb = new StringBuilder();

        for (var i = 0; i < _lineStartIndices.Count; i++)
        {
            sb.Clear();
            GetLineContent(i, sb);
            var lineWidth = _textMeasurer.MeasureText(sb, fontFamily, fontSize).Width;
            maxWidth = Math.Max(maxWidth, lineWidth);
        }

        return maxWidth;
    }

    private void GetLineContent(int lineIndex, StringBuilder sb)
    {
        var start = _lineStartIndices[lineIndex];
        var end = lineIndex < _lineStartIndices.Count - 1
            ? _lineStartIndices[lineIndex + 1]
            : TextBuffer.GetDocumentLength();

        var startChunk = _chunkStartIndices.BinarySearch(start);
        if (startChunk < 0) startChunk = ~startChunk - 1;
        var endChunk = _chunkStartIndices.BinarySearch(end);
        if (endChunk < 0) endChunk = ~endChunk - 1;

        for (var i = startChunk; i <= endChunk; i++)
        {
            var chunkStart = Math.Max(start, _chunkStartIndices[i]);
            var chunkEnd = i < endChunk ? _chunkStartIndices[i + 1] : end;
            sb.Append(TextBuffer.GetDocumentSlice(chunkStart, chunkEnd));
        }
    }

    private void UpdateLineIndices()
    {
        var documentLength = TextBuffer.GetDocumentLength();
        _lineStartIndices.Clear();
        _lineStartIndices.Add(0);
        _chunkStartIndices.Clear();
        _chunkStartIndices.Add(0);

        for (var chunkStart = 0; chunkStart < documentLength; chunkStart += ChunkSize)
        {
            var chunkEnd = Math.Min(chunkStart + ChunkSize, documentLength);
            var chunk = TextBuffer.GetDocumentSlice(chunkStart, chunkEnd);
            _chunkStartIndices.Add(chunkEnd);

            for (var i = 0; i < chunk.Length; i++)
                if (chunk[i] == '\n')
                    _lineStartIndices.Add(chunkStart + i + 1);
        }
    }

    private void UpdateLineIndicesAfterInsert(int position, string text)
    {
        var lineIndex = GetLineIndexFromCharacterIndex(position);
        for (var i = 0; i < text.Length; i++)
            if (text[i] == '\n')
            {
                _lineStartIndices.Insert(lineIndex + 1, position + i + 1);
                lineIndex++;
            }

        for (var i = lineIndex + 1; i < _lineStartIndices.Count; i++) _lineStartIndices[i] += text.Length;
    }

    private void UpdateLineIndicesAfterDelete(int position, int length)
    {
        var startLineIndex = GetLineIndexFromCharacterIndex(position);
        var endLineIndex = GetLineIndexFromCharacterIndex(position + length);

        _lineStartIndices.RemoveRange(startLineIndex + 1, endLineIndex - startLineIndex);

        for (var i = startLineIndex + 1; i < _lineStartIndices.Count; i++) _lineStartIndices[i] -= length;
    }

    private void InvalidateCache()
    {
        _cachedMaxLineWidth = -1;
    }
}
