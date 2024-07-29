using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.UI.Config;
using Color = Avalonia.Media.Color;
using Point = Avalonia.Point;
using Size = Avalonia.Size;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using Vector = Avalonia.Vector;

namespace meteor.UI.Controls;

public class EditorContentControl : Control
{
    private readonly IEditorViewModel _viewModel;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;
    private readonly AvaloniaEditorConfig _avaloniaConfig;
    private readonly ISyntaxHighlighter _syntaxHighlighter;

    private Size _totalSize;
    private readonly List<int> _lineStartOffsets = new();
    private int _documentVersion;
    private int _cachedDocumentLength;

    public Vector Offset { get; set; }
    public Size Viewport { get; set; }
    public event EventHandler<Point> CursorPositionChanged;

    public double LineHeight { get; }

    public EditorContentControl(IEditorViewModel viewModel, ITextMeasurer textMeasurer, IEditorConfig config,
        ISyntaxHighlighter syntaxHighlighter)
    {
        _viewModel = viewModel;
        _textMeasurer = textMeasurer;
        _config = config;
        _avaloniaConfig = new AvaloniaEditorConfig();
        _syntaxHighlighter = syntaxHighlighter;

        LineHeight = _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize) * _config.LineHeightMultiplier;
        ClipToBounds = false;
        
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
        _totalSize = new Size(maxLineWidth + 50, lineCount * LineHeight + LineHeight * 2);
    }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(_avaloniaConfig.BackgroundBrush, null, new Rect(Bounds.Size));

        var (startLine, endLine) = CalculateVisibleLineRange();
        var visibleContent = _viewModel.GetContentSlice(startLine, endLine);
        var lines = visibleContent.Split('\n');

        var textHeight = _textMeasurer.MeasureText("Xypg", _config.FontFamily, _config.FontSize).Height;
        var verticalOffset = (LineHeight - textHeight) / 2;

        var currentLine = _viewModel.GetCursorLine();

        context.PushClip(new Rect(Offset.X, Offset.Y, Viewport.Width, Viewport.Height));

        RenderLines(context, lines, startLine, currentLine, verticalOffset);

        RenderCursor(context, currentLine, _viewModel.GetCursorColumn());

        if (_viewModel.IsCompletionActive) RenderCompletionOverlay(context);
    }

    private void RenderCompletionOverlay(DrawingContext context)
    {
        var cursorPosition = _viewModel.GetCursorPosition();
        var completionItems = _viewModel.CompletionItems;
        var selectedIndex = _viewModel.SelectedCompletionIndex;

        // Calculate the position for the completion overlay
        var overlayX = cursorPosition.X;
        var overlayY = cursorPosition.Y + LineHeight;

        // Create a background for the completion overlay
        var backgroundBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240));
        var borderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        var itemHeight = 20;
        var overlayWidth = 200;
        var overlayHeight = completionItems.Count * itemHeight;

        context.DrawRectangle(backgroundBrush, new Pen(borderBrush),
            new Rect(overlayX, overlayY, overlayWidth, overlayHeight));

        // Render completion items
        for (var i = 0; i < completionItems.Count; i++)
        {
            var item = completionItems[i];
            var itemY = overlayY + i * itemHeight;

            if (i == selectedIndex)
            {
                var selectionBrush = new SolidColorBrush(Color.FromRgb(173, 214, 255));
                context.DrawRectangle(selectionBrush, null, new Rect(overlayX, itemY, overlayWidth, itemHeight));
            }

            var textBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            var formattedText = new FormattedText(
                item.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(_config.FontFamily),
                _config.FontSize,
                textBrush);

            context.DrawText(formattedText, new Point(overlayX + 5, itemY + (itemHeight - formattedText.Height) / 2));
        }
    }

    private (int startLine, int endLine) CalculateVisibleLineRange()
    {
        var startLine = Math.Max(0, (int)(Offset.Y / LineHeight));
        var visibleLines = (int)Math.Ceiling(Viewport.Height / LineHeight) + 1;
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

            var lineY = (startLine + i) * LineHeight;

            if (startLine + i == currentLine)
            {
                context.DrawRectangle(_avaloniaConfig.CurrentLineHighlightBrush, null,
                    new Rect(0, lineY, Bounds.Width, LineHeight));
            }

            RenderSelection(context, startLine + i, line, lineY);

            // Apply syntax highlighting
            var highlightedSegments = _syntaxHighlighter.HighlightSyntax(line, "csharp");
            var currentX = 0.0;

            foreach (var segment in highlightedSegments)
            {
                var formattedText = new FormattedText(
                    segment.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    _avaloniaConfig.Typeface,
                    _config.FontSize,
                    GetBrushForStyle(segment.Style));

                context.DrawText(formattedText, new Point(currentX, lineY + verticalOffset));
                currentX += formattedText.WidthIncludingTrailingWhitespace;
            }
        }
    }

    private IBrush GetBrushForStyle(string style)
    {
        return style switch
        {
            "keyword" => new SolidColorBrush(Color.FromRgb(0, 0, 205)), // Medium Blue
            "preprocessor" => new SolidColorBrush(Color.FromRgb(138, 43, 226)), // Blue Violet
            "comment" => new SolidColorBrush(Color.FromRgb(34, 139, 34)), // Forest Green
            "xmldoc" => new SolidColorBrush(Color.FromRgb(0, 128, 128)), // Teal
            "attribute" => new SolidColorBrush(Color.FromRgb(255, 69, 0)), // Orange Red
            "method" => new SolidColorBrush(Color.FromRgb(70, 130, 180)), // Steel Blue
            "string" => new SolidColorBrush(Color.FromRgb(220, 20, 60)), // Crimson
            "number" => new SolidColorBrush(Color.FromRgb(0, 128, 128)), // Teal
            "type" => new SolidColorBrush(Color.FromRgb(72, 61, 139)), // Dark Slate Blue
            "namespace" => new SolidColorBrush(Color.FromRgb(0, 0, 139)), // Dark Blue
            "linq" => new SolidColorBrush(Color.FromRgb(0, 0, 205)), // Medium Blue
            "operator" => new SolidColorBrush(Color.FromRgb(0, 0, 0)), // Black
            "lambda" => new SolidColorBrush(Color.FromRgb(0, 0, 205)), // Medium Blue
            "whitespace" => new SolidColorBrush(Color.FromRgb(255, 255, 255)), // White
            _ => _avaloniaConfig.TextBrush
        };
    }

    
    private void RenderCursor(DrawingContext context, int cursorLine, int cursorColumn)
    {
        var cursorY = cursorLine * LineHeight;

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
            new Point(cursorX, cursorY + LineHeight)
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
        if (!_viewModel.HasSelection()) return;

        var selectionStart = _viewModel.SelectionStart;
        var selectionEnd = _viewModel.SelectionEnd;

        var selectionStartLine = _viewModel.TextBufferService.GetLineIndexFromCharacterIndex(selectionStart);
        var selectionEndLine = _viewModel.TextBufferService.GetLineIndexFromCharacterIndex(selectionEnd);

        if (lineIndex < selectionStartLine || lineIndex > selectionEndLine) return;

        var lineStartOffset = _viewModel.GetLineStartOffset(lineIndex);

        var selectionStartInLine = Math.Max(0, selectionStart - lineStartOffset);
        var selectionEndInLine = Math.Min(line.Length, selectionEnd - lineStartOffset);

        if (selectionStartInLine < 0) selectionStartInLine = 0;
        if (selectionEndInLine > line.Length) selectionEndInLine = line.Length;
        
        if (line.Length == 0)
        {
            context.DrawRectangle(
                _avaloniaConfig.SelectionBrush,
                null,
                new Rect(0, lineY, 10, LineHeight)
            );
            return;
        }

        if (lineIndex == selectionStartLine && lineIndex == selectionEndLine)
            DrawSelectionRectangle(context, line, selectionStartInLine, selectionEndInLine, lineY);
        else if (lineIndex == selectionStartLine)
            DrawSelectionRectangle(context, line, selectionStartInLine, line.Length, lineY);
        else if (lineIndex == selectionEndLine)
            DrawSelectionRectangle(context, line, 0, selectionEndInLine, lineY);
        else
            DrawSelectionRectangle(context, line, 0, line.Length, lineY);
    }

    private void DrawSelectionRectangle(DrawingContext context, string line, int start, int end, double lineY)
    {
        start = Math.Max(0, Math.Min(start, line.Length));
        end = Math.Max(0, Math.Min(end, line.Length));

        if (start > end)
        {
            (start, end) = (end, start);
        }

        var startX = MeasureTextWidth(line.Substring(0, start));
        var endX = MeasureTextWidth(line.Substring(0, end));

        context.DrawRectangle(
            _avaloniaConfig.SelectionBrush,
            null,
            new Rect(startX, lineY, Math.Max(endX - startX, 1), LineHeight)
        );
    }

    private double MeasureTextWidth(string text)
    {
        return _textMeasurer.MeasureText(text, _config.FontFamily, _config.FontSize).Width;
    }
}