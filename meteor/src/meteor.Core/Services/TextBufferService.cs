using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class TextBufferService : ITextBufferService
{
    private readonly ITextMeasurer _textMeasurer;
    private double _cachedMaxLineWidth;
    private string _cachedFontFamily;
    private double _cachedFontSize;
    private readonly List<int> _lineStartIndices;
    private int _documentLength;
    private readonly Dictionary<int, double> _lineWidths;

    public TextBufferService(TextBuffer textBuffer, ITextMeasurer textMeasurer, IEditorConfig config)
    {
        _textMeasurer = textMeasurer;
        _cachedMaxLineWidth = -1;
        _cachedFontFamily = config.FontFamily;
        _cachedFontSize = config.FontSize > 0 ? config.FontSize : 13;
        _lineStartIndices = new List<int> { 0 };
        _documentLength = 0;
        _lineWidths = new Dictionary<int, double>();
        TextBuffer = textBuffer;
    }

    public TextBuffer TextBuffer { get; }

    public string GetContent()
    {
        return TextBuffer.GetDocumentSlice(0, _documentLength);
    }

    public int GetDocumentVersion()
    {
        return TextBuffer.GetVersion();
    }

    public string GetContentSliceByIndex(int startIndex, int length)
    {
        startIndex = Math.Clamp(startIndex, 0, _documentLength);
        length = Math.Clamp(length, 0, _documentLength - startIndex);
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
            lineIndex >= _lineStartIndices.Count ? _documentLength :
            _lineStartIndices[lineIndex];
    }

    public string GetContentSlice(int startLine, int endLine)
    {
        startLine = Math.Clamp(startLine, 0, _lineStartIndices.Count - 1);
        endLine = Math.Clamp(endLine, startLine, _lineStartIndices.Count - 1);

        var startIndex = _lineStartIndices[startLine];
        var endIndex = endLine < _lineStartIndices.Count - 1
            ? _lineStartIndices[endLine + 1]
            : _documentLength;

        return TextBuffer.GetDocumentSlice(startIndex, endIndex);
    }

    public void InsertText(int position, string text)
    {
        TextBuffer.InsertText(position, text);
        UpdateLineIndicesAfterInsert(position, text);
        _documentLength += text.Length;
        UpdateLineWidthsAfterInsert(position, text);
    }

    public void DeleteText(int position, int length)
    {
        TextBuffer.DeleteText(position, length);
        UpdateLineIndicesAfterDelete(position, length);
        _documentLength -= length;
        UpdateLineWidthsAfterDelete(position, length);
    }

    public int GetLength()
    {
        return _documentLength;
    }

    public int GetLineCount()
    {
        return _lineStartIndices.Count;
    }

    public void LoadContent(string content)
    {
        TextBuffer.LoadContent(content);
        _documentLength = TextBuffer.GetDocumentLength();
        RecalculateAllLineWidths(_cachedFontFamily, _cachedFontSize);
    }
    
    public double GetMaxLineWidth(string fontFamily, double fontSize)
    {
        fontFamily = string.Intern(fontFamily);
        if (!ReferenceEquals(fontFamily, _cachedFontFamily) || Math.Abs(fontSize - _cachedFontSize) > 0.001)
        {
            RecalculateAllLineWidths(fontFamily, fontSize);
        }
        return _cachedMaxLineWidth;
    }

    public int GetLineStartOffset(int lineIndex)
    {
        if (lineIndex < 0) return 0;
        if (lineIndex >= _lineStartIndices.Count) return _documentLength;
        return _lineStartIndices[lineIndex];
    }

    private void UpdateLineIndicesAfterInsert(int position, string text)
    {
        var lineIndex = GetLineIndexFromCharacterIndex(position);
        var newLineCount = text.Count(c => c == '\n');

        if (newLineCount > 0)
        {
            var newLineIndices = new List<int>();
            for (var i = 0; i < text.Length; i++)
                if (text[i] == '\n')
                {
                    newLineIndices.Add(position + i + 1);
                }

            _lineStartIndices.InsertRange(lineIndex + 1, newLineIndices);

            for (var i = lineIndex + newLineCount + 1; i < _lineStartIndices.Count; i++) _lineStartIndices[i] += text.Length;
        }
        else
        {
            for (var i = lineIndex + 1; i < _lineStartIndices.Count; i++) _lineStartIndices[i] += text.Length;
        }
    }

    private void UpdateLineIndicesAfterDelete(int position, int length)
    {
        var startLineIndex = GetLineIndexFromCharacterIndex(position);
        var endLineIndex = GetLineIndexFromCharacterIndex(position + length);

        _lineStartIndices.RemoveRange(startLineIndex + 1, endLineIndex - startLineIndex);

        for (var i = startLineIndex + 1; i < _lineStartIndices.Count; i++) _lineStartIndices[i] -= length;
    }

    private void UpdateLineWidthsAfterInsert(int position, string text)
    {
        var startLineIndex = GetLineIndexFromCharacterIndex(position);
        var endLineIndex = GetLineIndexFromCharacterIndex(position + text.Length);

        for (var i = startLineIndex; i <= endLineIndex; i++) UpdateLineWidth(i);

        UpdateMaxLineWidth();
    }

    private void UpdateLineWidthsAfterDelete(int position, int length)
    {
        var startLineIndex = GetLineIndexFromCharacterIndex(position);
        var endLineIndex = GetLineIndexFromCharacterIndex(position + length);

        for (var i = startLineIndex; i <= endLineIndex && i < _lineStartIndices.Count; i++) UpdateLineWidth(i);

        UpdateMaxLineWidth();
    }

    private void UpdateLineWidth(int lineIndex)
    {
        var lineContent = GetLineContent(lineIndex);
        var lineWidth = _textMeasurer.MeasureText(lineContent, _cachedFontFamily, _cachedFontSize).Width;
        _lineWidths[lineIndex] = lineWidth;

        if (lineWidth > _cachedMaxLineWidth)
        {
            _cachedMaxLineWidth = lineWidth;
        }
    }

    private void UpdateMaxLineWidth()
    {
        if (_lineWidths.Count == 0)
        {
            _cachedMaxLineWidth = 0;
            return;
        }

        var maxWidth = _lineWidths.Values.Max();
        _cachedMaxLineWidth = maxWidth;
    }

    private void RecalculateAllLineWidths(string fontFamily, double fontSize)
    {
        _cachedFontFamily = fontFamily;
        _cachedFontSize = fontSize;
        _lineWidths.Clear();

        for (var i = 0; i < _lineStartIndices.Count; i++) UpdateLineWidth(i);

        UpdateMaxLineWidth();
    }

    private string GetLineContent(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _lineStartIndices.Count)
            throw new ArgumentOutOfRangeException(nameof(lineIndex),
                "Specified lineIndex was out of the range of valid values.");

        var start = _lineStartIndices[lineIndex];
        var end = lineIndex < _lineStartIndices.Count - 1
            ? _lineStartIndices[lineIndex + 1]
            : _documentLength;

        return TextBuffer.GetDocumentSlice(start, end);
    }

}