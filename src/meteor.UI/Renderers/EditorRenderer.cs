using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.Core.Enums.SyntaxHighlighting;
using meteor.Core.Models.SyntaxHighlighting;
using meteor.UI.Services;

namespace meteor.UI.Renderers;

public class EditorRenderer
{
    private readonly IBrush _keywordBrush = Brushes.Blue;
    private readonly IBrush _commentBrush = Brushes.Green;
    private readonly IBrush _stringBrush = Brushes.Red;
    private readonly IBrush _plainTextBrush = Brushes.Black;
    private readonly IBrush _selectionBrush = new SolidColorBrush(Color.FromArgb(128, 173, 214, 255));
    private readonly IPen _cursorPen = new Pen(Brushes.Black);
    private readonly double _fontSize = 13;
    private readonly Typeface _typeface = new("Consolas");
    private readonly AvaloniaTextMeasurer _textMeasurer;

    private bool _showCursor = true;
    private DispatcherTimer _cursorBlinkTimer;
    private readonly Action _invalidateView;

    public EditorRenderer(Action invalidateView)
    {
        _textMeasurer = new AvaloniaTextMeasurer(_typeface, _fontSize);
        _invalidateView = invalidateView;

        _cursorBlinkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _cursorBlinkTimer.Tick += (sender, args) =>
        {
            _showCursor = !_showCursor;
            _invalidateView();
        };
        _cursorBlinkTimer.Start();
    }

    public void Render(DrawingContext context, Rect bounds, string text,
        IEnumerable<SyntaxHighlightingResult> highlightingResults,
        (int start, int length) selection, int cursorPosition,
        int firstVisibleLine, int visibleLineCount,
        double offsetX, double offsetY)
    {
        context.DrawRectangle(Brushes.White, null, bounds);

        var lineHeight = _textMeasurer.GetLineHeight();
        var currentIndex = 0;
        var lineNumber = 0;

        while (currentIndex < text.Length && lineNumber < firstVisibleLine + visibleLineCount)
        {
            var lineEndIndex = text.IndexOf('\n', currentIndex);
            if (lineEndIndex == -1) lineEndIndex = text.Length;

            if (lineNumber >= firstVisibleLine)
            {
                var line = text.Substring(currentIndex, lineEndIndex - currentIndex);
                var lineY = (lineNumber - firstVisibleLine) * lineHeight + offsetY;

                if (lineY >= bounds.Top && lineY < bounds.Bottom)
                    RenderLine(context, text, line, lineNumber, currentIndex, lineY, highlightingResults, selection,
                        cursorPosition, offsetX);
            }

            currentIndex = lineEndIndex + 1;
            lineNumber++;
        }
    }

    private void RenderLine(DrawingContext context, string fullText, string line, int lineNumber, int lineStart,
        double lineY,
        IEnumerable<SyntaxHighlightingResult> highlightingResults,
        (int start, int length) selection, int cursorPosition, double offsetX)
    {
        DrawLineSelection(context, fullText, line, lineStart, lineY, selection, offsetX);

        var formattedText = new FormattedText(
            line,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            _plainTextBrush
        );

        ApplySyntaxHighlighting(formattedText, line, lineStart, highlightingResults);

        context.DrawText(formattedText, new Point(offsetX, lineY));

        if (_showCursor && cursorPosition >= lineStart && cursorPosition <= lineStart + line.Length)
        {
            var cursorX = _textMeasurer.GetPositionAtIndex(line, cursorPosition - lineStart).x;
            context.DrawLine(_cursorPen,
                new Point(cursorX + offsetX, lineY),
                new Point(cursorX + offsetX, lineY + _textMeasurer.GetLineHeight()));
        }
    }

    private void DrawLineSelection(DrawingContext context, string fullText, string line, int lineStart, double lineY,
        (int start, int length) selection, double offsetX)
    {
        var lineEnd = lineStart + line.Length;

        if (selection.length != 0)
        {
            var selectionStart = selection.start;
            var selectionEnd = selection.start + selection.length;

            if (!(selectionEnd <= lineStart || selectionStart >= lineEnd))
            {
                var startX = selectionStart > lineStart
                    ? _textMeasurer.GetPositionAtIndex(line, selectionStart - lineStart).x
                    : 0;
                var endX = selectionEnd < lineEnd
                    ? _textMeasurer.GetPositionAtIndex(line, selectionEnd - lineStart).x
                    : _textMeasurer.GetStringWidth(line);

                context.DrawRectangle(_selectionBrush, null,
                    new Rect(startX + offsetX, lineY, endX - startX, _textMeasurer.GetLineHeight()));
            }
        }
    }

    private void ApplySyntaxHighlighting(FormattedText formattedText, string line, int lineStart,
        IEnumerable<SyntaxHighlightingResult> highlightingResults)
    {
        foreach (var result in highlightingResults)
            if (result.StartIndex + result.Length > lineStart && result.StartIndex < lineStart + line.Length)
            {
                var highlightStart = Math.Max(0, result.StartIndex - lineStart);
                var highlightEnd = Math.Min(line.Length, result.StartIndex + result.Length - lineStart);
                var brush = GetBrushForHighlightingType(result.Type);
                formattedText.SetForegroundBrush(brush, highlightStart, highlightEnd - highlightStart);
            }
    }

    private IBrush GetBrushForHighlightingType(SyntaxHighlightingType type)
    {
        return type switch
        {
            SyntaxHighlightingType.Keyword => _keywordBrush,
            SyntaxHighlightingType.Comment => _commentBrush,
            SyntaxHighlightingType.String => _stringBrush,
            _ => _plainTextBrush
        };
    }

    public void ResetCursorBlink()
    {
        _showCursor = true;
        _cursorBlinkTimer.Stop();
        _cursorBlinkTimer.Start();
        _invalidateView();
    }

    public void Dispose()
    {
        _cursorBlinkTimer.Stop();
        _cursorBlinkTimer = null;
    }

    public (int firstVisibleLine, int visibleLineCount) CalculateVisibleLines(double viewportHeight,
        double scrollOffset)
    {
        var lineHeight = _textMeasurer.GetLineHeight();
        var firstVisibleLine = (int)(scrollOffset / lineHeight);
        var visibleLineCount = (int)(viewportHeight / lineHeight) + 1;
        return (firstVisibleLine, visibleLineCount);
    }
}