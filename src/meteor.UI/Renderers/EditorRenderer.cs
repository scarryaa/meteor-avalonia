using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.Core.Enums.SyntaxHighlighting;
using meteor.Core.Interfaces.Services;
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

    private const int BufferLines = 5; // Number of extra lines to render above and below the visible area

    private readonly List<(int start, int length)> _lineInfo = new();
    private int _totalLines;

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

    public void UpdateLineInfo(ITextBufferService textBufferService)
    {
        _lineInfo.Clear();
        _totalLines = 0;
        var currentIndex = 0;

        while (currentIndex < textBufferService.Length)
        {
            var lineEndIndex = textBufferService.IndexOf('\n', currentIndex);
            if (lineEndIndex == -1) lineEndIndex = textBufferService.Length;

            var lineLength = lineEndIndex - currentIndex;
            _lineInfo.Add((currentIndex, lineLength));
            _totalLines++;

            currentIndex = lineEndIndex + 1;
        }
    }

    public void Render(DrawingContext context, Rect bounds, ITextBufferService textBufferService,
        IEnumerable<SyntaxHighlightingResult> highlightingResults,
        (int start, int length) selection, int cursorPosition,
        double scrollOffset, double offsetX)
    {
        context.DrawRectangle(Brushes.White, null, bounds);

        var lineHeight = _textMeasurer.GetLineHeight();
        var firstVisibleLine = Math.Max(0, (int)(scrollOffset / lineHeight) - BufferLines);
        var lastVisibleLine =
            Math.Min(_totalLines - 1, (int)((scrollOffset + bounds.Height) / lineHeight) + BufferLines);

        for (var lineNumber = firstVisibleLine; lineNumber <= lastVisibleLine; lineNumber++)
        {
            if (lineNumber >= _lineInfo.Count) break;

            var (lineStart, lineLength) = _lineInfo[lineNumber];
            var lineY = lineNumber * lineHeight - scrollOffset;

            RenderLine(context, textBufferService, lineNumber, lineStart, lineLength, lineY,
                highlightingResults, selection, cursorPosition, offsetX);
        }
    }

    private void RenderLine(DrawingContext context, ITextBufferService textBufferService, int lineNumber, int lineStart,
        int lineLength, double lineY, IEnumerable<SyntaxHighlightingResult> highlightingResults,
        (int start, int length) selection, int cursorPosition, double offsetX)
    {
        DrawLineSelection(context, textBufferService, lineStart, lineLength, lineY, selection, offsetX);

        var sb = new StringBuilder(lineLength);
        textBufferService.GetTextSegment(lineStart, lineLength, sb);
        
        var formattedText = new FormattedText(
            sb.ToString(),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            _plainTextBrush
        );

        formattedText.MaxTextWidth = double.PositiveInfinity;

        ApplySyntaxHighlighting(formattedText, lineStart, lineLength, highlightingResults);

        context.DrawText(formattedText, new Point(-offsetX, lineY));

        if (_showCursor && cursorPosition >= lineStart && cursorPosition <= lineStart + lineLength)
        {
            var cursorX = _textMeasurer.GetPositionAtIndex(sb.ToString(), cursorPosition - lineStart).x;
            context.DrawLine(_cursorPen,
                new Point(cursorX - offsetX, lineY),
                new Point(cursorX - offsetX, lineY + _textMeasurer.GetLineHeight()));
        }
    }


    private void DrawLineSelection(DrawingContext context, ITextBufferService textBufferService, int lineStart,
        int lineLength, double lineY, (int start, int length) selection, double offsetX)
    {
        var lineEnd = lineStart + lineLength;

        if (selection.length != 0)
        {
            var selectionStart = selection.start;
            var selectionEnd = selection.start + selection.length;

            if (!(selectionEnd <= lineStart || selectionStart >= lineEnd))
            {
                var startX = selectionStart > lineStart
                    ? GetXPositionForIndex(textBufferService, lineStart, selectionStart - lineStart)
                    : 0;
                var endX = selectionEnd < lineEnd
                    ? GetXPositionForIndex(textBufferService, lineStart, selectionEnd - lineStart)
                    : GetXPositionForIndex(textBufferService, lineStart, lineLength);

                context.DrawRectangle(_selectionBrush, null,
                    new Rect(startX - offsetX, lineY, endX - startX, _textMeasurer.GetLineHeight()));
            }
        }
    }

    private double GetXPositionForIndex(ITextBufferService textBufferService, int start, int length)
    {
        var sb = new StringBuilder(length);
        textBufferService.GetTextSegment(start, length, sb);
        return _textMeasurer.GetStringWidth(sb.ToString());
    }

    private void ApplySyntaxHighlighting(FormattedText formattedText, int lineStart, int lineLength,
        IEnumerable<SyntaxHighlightingResult> highlightingResults)
    {
        foreach (var result in highlightingResults)
            if (result.StartIndex + result.Length > lineStart && result.StartIndex < lineStart + lineLength)
            {
                var highlightStart = Math.Max(0, result.StartIndex - lineStart);
                var highlightEnd = Math.Min(lineLength, result.StartIndex + result.Length - lineStart);
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