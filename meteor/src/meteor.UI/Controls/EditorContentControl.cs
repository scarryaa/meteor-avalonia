using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.UI.Config;
using Point = Avalonia.Point;
using Size = Avalonia.Size;
using Vector = Avalonia.Vector;

namespace meteor.UI.Controls;

public class EditorContentControl : Control
{
    private readonly IEditorViewModel _viewModel;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;
    private readonly AvaloniaEditorConfig _avaloniaConfig;
    
    private readonly double _lineHeight;
    private Size _totalSize;
    private readonly List<int> _lineStartOffsets = new();
    private int _documentVersion;
    private int _cachedDocumentLength;

    public Vector Offset { get; set; }
    public Size Viewport { get; set; }

    public EditorContentControl(IEditorViewModel viewModel, ITextMeasurer textMeasurer, IEditorConfig config)
    {
        _viewModel = viewModel;
        _textMeasurer = textMeasurer;
        _config = config;
        _avaloniaConfig = new AvaloniaEditorConfig();
        
        _lineHeight = _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize) * _config.LineHeightMultiplier;

        _viewModel.ContentChanged += (_, _) =>
        {
            UpdateLineStartOffsets();
        };
        _viewModel.SelectionChanged += (_, _) => InvalidateVisual();
        UpdateLineStartOffsets();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        UpdateContentMeasurements();
        return _totalSize;
    }

    private void UpdateContentMeasurements()
    {
        var lineCount = _viewModel.GetLineCount();
        var maxLineWidth = _viewModel.GetMaxLineWidth();
        _totalSize = new Size(maxLineWidth + 50, lineCount * _lineHeight + _lineHeight * 2);
    }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(_avaloniaConfig.BackgroundBrush, null, new Rect(Bounds.Size));

        var (startLine, endLine) = CalculateVisibleLineRange();
        var visibleContent = _viewModel.GetContentSlice(startLine, endLine);
        var lines = visibleContent.Split('\n');

        var textHeight = _textMeasurer.MeasureText("Xypg", _config.FontFamily, _config.FontSize).Height;
        var verticalOffset = (_lineHeight - textHeight) / 2;

        var currentLine = _viewModel.GetCursorLine();

        context.PushClip(new Rect(Offset.X, Offset.Y, Viewport.Width, Viewport.Height));

        RenderLines(context, lines, startLine, currentLine, verticalOffset);

        RenderCursor(context, currentLine, _viewModel.GetCursorColumn());
    }

    private (int startLine, int endLine) CalculateVisibleLineRange()
    {
        var startLine = Math.Max(0, (int)(Offset.Y / _lineHeight));
        var visibleLines = (int)Math.Ceiling(Viewport.Height / _lineHeight) + 1;
        var endLine = Math.Min(_viewModel.GetLineCount() - 1, startLine + visibleLines);

        var bufferLines = 10;
        return (
            Math.Clamp(startLine - bufferLines, 0, _viewModel.GetLineCount() - 1),
            Math.Clamp(endLine + bufferLines, 0, _viewModel.GetLineCount() - 1)
        );
    }

    private void RenderLines(DrawingContext context, string[] lines, int startLine, int currentLine,
        double verticalOffset)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null) continue;

            var lineY = (startLine + i) * _lineHeight;

            if (startLine + i == currentLine)
            {
                context.DrawRectangle(_avaloniaConfig.CurrentLineHighlightBrush, null,
                    new Rect(0, lineY, Bounds.Width, _lineHeight));
            }

            RenderSelection(context, startLine + i, line, lineY);

            var formattedText = new FormattedText(
                line,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _avaloniaConfig.Typeface,
                _config.FontSize,
                _avaloniaConfig.TextBrush);

            context.DrawText(formattedText, new Point(0, lineY + verticalOffset));
        }
    }

    private void RenderCursor(DrawingContext context, int cursorLine, int cursorColumn)
    {
        var cursorY = cursorLine * _lineHeight;

        var lineContent = _viewModel.GetContentSlice(cursorLine, cursorLine);

        // Ensure cursorColumn is within the bounds of the line content
        cursorColumn = Math.Min(cursorColumn, lineContent.Length);

        var cursorX = 0.0;
        if (cursorColumn > 0)
            try
            {
                cursorX = MeasureTextWidth(lineContent.Substring(0, cursorColumn));
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine(
                    $"Error measuring text width. Line: {cursorLine}, Column: {cursorColumn}, Content length: {lineContent.Length}");
                cursorX = MeasureTextWidth(lineContent);
            }

        context.DrawLine(
            new Pen(_avaloniaConfig.TextBrush),
            new Point(cursorX, cursorY),
            new Point(cursorX, cursorY + _lineHeight)
        );
    }

    private void UpdateLineStartOffsets(TextChange? change = null)
    {
        var currentDocumentLength = _viewModel.GetDocumentLength();
        var currentDocumentVersion = _viewModel.GetDocumentVersion();

        // If the document hasn't changed, use the cached offsets
        if (currentDocumentVersion == _documentVersion && currentDocumentLength == _cachedDocumentLength) return;

        // If it's a new document or the change is too large, recalculate everything
        if (_lineStartOffsets.Count == 0 || change == null || IsLargeChange(change))
            RecalculateAllOffsets();
        else
            // Perform an incremental update
            UpdateOffsetsIncrementally(change);

        _documentVersion = currentDocumentVersion;
        _cachedDocumentLength = currentDocumentLength;
    }

    private void RecalculateAllOffsets()
    {
        _lineStartOffsets.Clear();
        _lineStartOffsets.Add(0);

        var documentLength = _viewModel.GetDocumentLength();
        const int chunkSize = 16384;

        for (var chunkStart = 0; chunkStart < documentLength; chunkStart += chunkSize)
        {
            var chunkEnd = Math.Min(chunkStart + chunkSize, documentLength);
            var chunk = _viewModel.GetContentSlice(chunkStart, chunkEnd);

            foreach (var index in FindNewLineIndexes(chunk))
            {
                var totalLength = chunkStart + index + 1;
                _lineStartOffsets.Add(totalLength);
            }
        }
    }

    private void UpdateOffsetsIncrementally(TextChange change)
    {
        var changeStart = change.Offset;
        var changeEnd = change.Offset + change.OldLength;
        var changeDelta = change.NewLength - change.OldLength;

        // Find the affected line range
        var startLineIndex = _lineStartOffsets.BinarySearch(changeStart);
        if (startLineIndex < 0) startLineIndex = ~startLineIndex - 1;

        var endLineIndex = _lineStartOffsets.BinarySearch(changeEnd);
        if (endLineIndex < 0) endLineIndex = ~endLineIndex;

        // Update offsets after the change
        for (var i = endLineIndex; i < _lineStartOffsets.Count; i++) _lineStartOffsets[i] += changeDelta;

        // Recalculate offsets for the affected lines
        var newOffsets = new List<int>();
        var chunkStart = startLineIndex > 0 ? _lineStartOffsets[startLineIndex - 1] : 0;
        var chunkEnd = endLineIndex < _lineStartOffsets.Count
            ? _lineStartOffsets[endLineIndex] + changeDelta
            : _viewModel.GetDocumentLength();

        var chunk = _viewModel.GetContentSlice(chunkStart, chunkEnd);
        foreach (var index in FindNewLineIndexes(chunk)) newOffsets.Add(chunkStart + index + 1);

        // Replace the old offsets with the new ones
        _lineStartOffsets.RemoveRange(startLineIndex, endLineIndex - startLineIndex);
        _lineStartOffsets.InsertRange(startLineIndex, newOffsets);
    }

    private bool IsLargeChange(TextChange change)
    {
        return change.OldLength > _viewModel.GetDocumentLength() * 0.1;
    }

    private IEnumerable<int> FindNewLineIndexes(string text)
    {
        var index = -1;
        while ((index = text.IndexOf('\n', index + 1)) != -1) yield return index;
    }

    private void RenderSelection(DrawingContext context, int lineIndex, string line, double lineY)
    {
        var (selectionStart, selectionEnd) = (_viewModel.SelectionStart, _viewModel.SelectionEnd);

        if (selectionStart == selectionEnd || selectionEnd <= selectionStart)
            return;

        var lineStartOffset = _lineStartOffsets[lineIndex];

        if (selectionEnd <= lineStartOffset || selectionStart >= lineStartOffset + line.Length)
            return;

        var relativeSelectionStart = Math.Max(0, selectionStart - lineStartOffset);
        var relativeSelectionEnd = Math.Min(line.Length, selectionEnd - lineStartOffset);

        if (relativeSelectionStart < relativeSelectionEnd || string.IsNullOrEmpty(line))
        {
            var startX = MeasureTextWidth(line.Substring(0, relativeSelectionStart));
            var endX = MeasureTextWidth(line.Substring(0, relativeSelectionEnd));

            if (string.IsNullOrEmpty(line))
            {
                startX = 0;
                endX = 10;
            }

            if (startX < endX)
            {
                context.DrawRectangle(_avaloniaConfig.SelectionBrush, null,
                    new Rect(startX, lineY, endX - startX, _lineHeight));
            }
        }
    }

    private double MeasureTextWidth(string text)
    {
        return _textMeasurer.MeasureText(text, _config.FontFamily, _config.FontSize).Width;
    }
}