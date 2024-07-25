using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.UI.Controls;

public class EditorContentControl : Control
{
    private readonly IEditorViewModel _viewModel;
    private readonly ITextMeasurer _textMeasurer;
    private const double FontSize = 13;
    private readonly Typeface _typeface = new("Consolas");
    private readonly double _lineHeight;
    private Size _totalSize;

    public Vector Offset { get; set; }
    public Size Viewport { get; set; }

    public EditorContentControl(IEditorViewModel viewModel, ITextMeasurer textMeasurer)
    {
        _viewModel = viewModel;
        _textMeasurer = textMeasurer;
        _lineHeight = _textMeasurer.GetLineHeight(_typeface.FontFamily.ToString(), FontSize) * 1.5;

        _viewModel.ContentChanged += (sender, e) => InvalidateVisual();
        _viewModel.SelectionChanged += (sender, e) => InvalidateVisual();
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
        _totalSize = new Size(maxLineWidth, lineCount * _lineHeight);
    }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(Brushes.White, null, new Rect(Bounds.Size));

        var startLine = Math.Max(0, (int)(Offset.Y / _lineHeight));
        var visibleLines = (int)Math.Ceiling(Viewport.Height / _lineHeight) + 1;
        var endLine = Math.Min(_viewModel.GetLineCount() - 1, startLine + visibleLines);

        var bufferLines = 10;
        var fetchStartLine = Math.Max(0, startLine - bufferLines);
        var fetchEndLine = Math.Min(_viewModel.GetLineCount() - 1, endLine + bufferLines);

        var visibleContent = _viewModel.GetContentSlice(fetchStartLine, fetchEndLine);
        var lines = visibleContent.Split('\n');

        var textHeight = _textMeasurer.MeasureText("Xypg", _typeface.FontFamily.ToString(), FontSize).Height;
        var verticalOffset = (_lineHeight - textHeight) / 2;

        var currentLine = _viewModel.GetCursorLine();

        var contentOffset = _viewModel.GetContentSlice(0, fetchStartLine).Length;

        var yOffset = -(Offset.Y % _lineHeight);
        
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null) continue;

            var verticalCenterOffset = (_lineHeight - textHeight) / 2;
            var lineY = (fetchStartLine + i) * _lineHeight + verticalCenterOffset;
            var lineStartOffset = fetchStartLine + lines.Take(i).Sum(l => l.Length + 1);

            // Highlight the current line
            if (fetchStartLine + i == currentLine)
            {
                var highlightBrush = new SolidColorBrush(Color.Parse("#ededed"));
                context.DrawRectangle(highlightBrush, null, new Rect(0, lineY, Bounds.Width, _lineHeight));
            }

            // Render selection
            RenderSelection(context, lineStartOffset, line, lineY);

            var formattedText = new FormattedText(
                line,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                FontSize,
                Brushes.Black);

            var textY = lineY + verticalOffset;
            context.DrawText(formattedText, new Point(0, textY));

            contentOffset += line.Length + 1; // +1 for the newline character
        }

        // Render cursor 
        RenderCursor(context, currentLine, _viewModel.GetCursorColumn());
    }

    private void RenderSelection(DrawingContext context, int lineStartOffset, string line, double lineY)
    {
        var selectionStart = _viewModel.SelectionStart;
        var selectionEnd = _viewModel.SelectionEnd;

        if (selectionStart == selectionEnd) return;

        var lineEndOffset = lineStartOffset + line.Length;

        // Check if any part of the selection is in this line
        if (selectionStart < lineEndOffset && selectionEnd > lineStartOffset)
        {
            var selectionBrush = new SolidColorBrush(Color.FromArgb(128, 173, 214, 255));

            var startX = Math.Max(0, selectionStart - lineStartOffset);
            var endX = Math.Min(line.Length, selectionEnd - lineStartOffset);

            var startXPosition = 0.0;
            if (startX > 0 && startX <= line.Length)
                startXPosition = _textMeasurer.MeasureText(
                    line.Substring(0, startX),
                    _typeface.FontFamily.ToString(),
                    FontSize
                ).Width;

            var endXPosition = Bounds.Width;
            if (endX >= 0 && endX <= line.Length)
                endXPosition = _textMeasurer.MeasureText(
                    line.Substring(0, endX),
                    _typeface.FontFamily.ToString(),
                    FontSize
                ).Width;

            var selectionWidth = Math.Max(
                endXPosition - startXPosition,
                _textMeasurer.MeasureText("A", _typeface.FontFamily.ToString(), FontSize).Width
            );

            context.DrawRectangle(
                selectionBrush,
                null,
                new Rect(startXPosition, lineY, selectionWidth, _lineHeight)
            );
        }
    }

    private void RenderCursor(DrawingContext context, int cursorLine, int cursorColumn)
    {
        var cursorY = cursorLine * _lineHeight;
        var cursorX = _textMeasurer.MeasureText(
            _viewModel.GetContentSlice(cursorLine, cursorLine).Substring(0, cursorColumn),
            _typeface.FontFamily.ToString(), FontSize).Width;

        context.DrawLine(
            new Pen(Brushes.Black),
            new Point(cursorX, cursorY),
            new Point(cursorX, cursorY + _lineHeight)
        );
    }
}
