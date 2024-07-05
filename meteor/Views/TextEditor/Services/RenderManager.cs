using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using meteor.Enums;
using meteor.Interfaces;
using meteor.Models;
using meteor.ViewModels;
using meteor.Views.Contexts;
using meteor.Views.Models;

namespace meteor.Views.Services;

public class RenderManager
{
    private readonly TextEditorContext _context;
    private readonly IThemeService _themeService;
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly ConcurrentDictionary<int, RenderedLine> _lineCache;
    private List<SyntaxToken> _cachedSyntaxTokens;
    private string _lastProcessedText;
    private Task _backgroundHighlightTask;

    public RenderManager(TextEditorContext context, IThemeService themeService, ISyntaxHighlighter syntaxHighlighter)
    {
        _context = context;
        _themeService = themeService;
        _syntaxHighlighter = syntaxHighlighter;
        _lineCache = new ConcurrentDictionary<int, RenderedLine>();
        _cachedSyntaxTokens = new List<SyntaxToken>();
    }

    public void UpdateContextViewModel(ScrollableTextEditorViewModel viewModel)
    {
        _context.ScrollableViewModel = viewModel;
    }

    public void AttachToViewModel(ScrollableTextEditorViewModel viewModel)
    {
        viewModel.ParentRenderManager = this;
        UpdateContextViewModel(viewModel);
    }

    public async Task InitializeAsync(string initialText)
    {
        await UpdateSyntaxHighlightingAsync(initialText);
        _context.ScrollableViewModel?.TextEditorViewModel.OnInvalidateRequired();
    }

    public void InvalidateLines(int startLine, int endLine)
    {
        for (var i = startLine; i <= endLine; i++) _lineCache.TryRemove(i, out _);
    }

    public void Render(DrawingContext context)
    {
        using (new PerformanceLogger("RenderManager.Render"))
        {
            var adjustedHeight = _context.ScrollableViewModel.Viewport.Height +
                                 _context.ScrollableViewModel.VerticalOffset;
            var adjustedWidth = _context.ScrollableViewModel.Viewport.Width +
                                _context.ScrollableViewModel.HorizontalOffset;
            context.FillRectangle(_context.BackgroundBrush, new Rect(new Size(adjustedWidth, adjustedHeight)));

            var lineCount = _context.ScrollableViewModel.TextEditorViewModel.TextBuffer.LineCount;
            if (lineCount == 0) return;

            var viewableAreaWidth = _context.ScrollableViewModel.Viewport.Width + _context.LinePadding +
                                    _context.ScrollableViewModel.HorizontalOffset;
            var viewableAreaHeight = _context.ScrollableViewModel.Viewport.Height;

            var firstVisibleLine = Math.Max(0,
                (int)(_context.ScrollableViewModel.VerticalOffset / _context.LineHeight) - 1);
            var lastVisibleLine = Math.Min(firstVisibleLine + (int)(viewableAreaHeight / _context.LineHeight) + 2,
                lineCount);

            var viewModel = _context.ScrollableViewModel.TextEditorViewModel;
            RenderCurrentLine(context, viewModel, viewableAreaWidth);

            RenderVisibleLines(context, _context.ScrollableViewModel, firstVisibleLine, (int)lastVisibleLine,
                viewableAreaWidth);
            DrawSelection(context, viewableAreaHeight, _context.ScrollableViewModel);
            DrawCursor(context, _context.ScrollableViewModel);
        }
    }

    private void RenderCurrentLine(DrawingContext context, TextEditorViewModel viewModel, double viewableAreaWidth)
    {
        var cursorLine = viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.CursorPosition);
        var y = cursorLine * _context.LineHeight;

        var selectionStartLine = viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.SelectionStart);
        var selectionEndLine = viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.SelectionEnd);

        var totalWidth = Math.Max(viewModel.WindowWidth,
            viewableAreaWidth + _context.ScrollableViewModel.HorizontalOffset!);

        if (cursorLine < selectionStartLine || cursorLine > selectionEndLine)
        {
            var rect = new Rect(0, y, totalWidth, _context.LineHeight);
            context.FillRectangle(_context.LineHighlightBrush, rect);
        }
        else
        {
            var selectionStart = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var selectionEnd = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);

            var lineStartOffset = cursorLine == selectionStartLine
                ? selectionStart - viewModel.TextBuffer.LineStarts[(int)cursorLine]
                : 0;
            var lineEndOffset = cursorLine == selectionEndLine
                ? selectionEnd - viewModel.TextBuffer.LineStarts[(int)cursorLine]
                : viewModel.TextBuffer.GetVisualLineLength((int)cursorLine);

            var xStart = lineStartOffset * viewModel.CharWidth;
            var xEnd = lineEndOffset * viewModel.CharWidth;

            if (xStart > 0)
            {
                var beforeSelectionRect = new Rect(0, y, xStart, _context.LineHeight);
                context.FillRectangle(_context.LineHighlightBrush, beforeSelectionRect);
            }

            var afterSelectionRect = new Rect(xEnd, y, totalWidth - xEnd, _context.LineHeight);
            context.FillRectangle(_context.LineHighlightBrush, afterSelectionRect);
        }
    }

    private void RenderVisibleLines(DrawingContext context, ScrollableTextEditorViewModel scrollableViewModel,
        int firstVisibleLine, int lastVisibleLine, double viewableAreaWidth)
    {
        using (new PerformanceLogger("RenderManager.RenderVisibleLines"))
        {
            var yOffset = firstVisibleLine * _context.LineHeight;
            var viewModel = scrollableViewModel.TextEditorViewModel;

            for (var i = firstVisibleLine; i < lastVisibleLine; i++)
            {
                var renderedLine =
                    _lineCache.GetOrAdd(i, lineIndex => RenderLine(viewModel, lineIndex, viewableAreaWidth));

                if (renderedLine.NeedsUpdate(scrollableViewModel.HorizontalOffset, viewableAreaWidth))
                {
                    renderedLine = RenderLine(viewModel, i, viewableAreaWidth);
                    _lineCache[i] = renderedLine;
                }

                context.DrawImage(renderedLine.Image,
                    new Rect(0, yOffset, renderedLine.Image.Size.Width, _context.LineHeight));
                yOffset += _context.LineHeight;
            }
        }
    }

    private RenderedLine RenderLine(TextEditorViewModel viewModel, int lineIndex, double viewableAreaWidth)
    {
        using (new PerformanceLogger($"RenderManager.RenderLine({lineIndex})"))
        {
            try
            {
                var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
                if (string.IsNullOrEmpty(lineText))
                    return new RenderedLine(
                        new WriteableBitmap(new PixelSize(1, (int)_context.LineHeight), new Vector(96, 96)), 0,
                        viewableAreaWidth);

                var horizontalOffset = (int)viewModel._scrollableViewModel.HorizontalOffset;
                var startIndex = (int)(horizontalOffset / viewModel.CharWidth);

                // Ensure startIndex is not larger than lineText.Length
                startIndex = Math.Min(startIndex, lineText.Length);

                var maxCharsToDisplay = (int)(viewableAreaWidth / viewModel.CharWidth) + 1;
                var visiblePartLength = Math.Min(maxCharsToDisplay, lineText.Length - startIndex);

                // Ensure visiblePartLength is not negative
                visiblePartLength = Math.Max(0, visiblePartLength);

                var pixelSize = new PixelSize((int)viewableAreaWidth, (int)_context.LineHeight);
                var renderTarget = new RenderTargetBitmap(pixelSize, new Vector(96, 96));

                using (var context = renderTarget.CreateDrawingContext())
                {
                    // Clear the background
                    context.FillRectangle(Brushes.Transparent, new Rect(0, 0, pixelSize.Width, pixelSize.Height));

                    // Calculate the x-offset for rendering text
                    var xOffset = startIndex * viewModel.CharWidth;

                    RenderLineWithSyntaxHighlighting(context, lineText, _cachedSyntaxTokens, lineIndex, startIndex, 0,
                        viewModel.CharWidth, visiblePartLength, xOffset);
                }

                var writeableBitmap = new WriteableBitmap(pixelSize, new Vector(96, 96));
                using (var lockedBitmap = writeableBitmap.Lock())
                {
                    renderTarget.CopyPixels(new PixelRect(0, 0, pixelSize.Width, pixelSize.Height),
                        lockedBitmap.Address,
                        lockedBitmap.RowBytes * lockedBitmap.Size.Height,
                        lockedBitmap.RowBytes);
                }

                return new RenderedLine(writeableBitmap, horizontalOffset, viewableAreaWidth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RenderLine for line {lineIndex}: {ex.Message}");
                // Return a blank bitmap in case of error
                return new RenderedLine(
                    new WriteableBitmap(new PixelSize(1, (int)_context.LineHeight), new Vector(96, 96)), 0,
                    viewableAreaWidth);
            }
        }
    }

    private void RenderLineWithSyntaxHighlighting(DrawingContext context, string lineText, List<SyntaxToken> allTokens,
        int lineIndex, int startIndex, double yOffset, double charWidth, int visibleLength, double xOffset)
    {
        var currentIndex = startIndex;
        var endIndex = Math.Min(startIndex + visibleLength, lineText.Length);

        var lineTokens = allTokens.Where(t => t.Line == lineIndex).OrderBy(t => t.StartColumn).ToList();

        if (lineTokens.Count == 0)
        {
            // If there are no tokens, render the entire visible part of the line as plain text
            var visibleText = lineText.Substring(startIndex, endIndex - startIndex);
            RenderText(context, visibleText, xOffset, yOffset, _context.Foreground, charWidth);
            return;
        }

        foreach (var token in lineTokens)
        {
            if (token.StartColumn >= endIndex) break;

            if (token.StartColumn + token.Length <= startIndex) continue;

            if (currentIndex < token.StartColumn)
            {
                var plainText = lineText.Substring(currentIndex, Math.Min(token.StartColumn, endIndex) - currentIndex);
                RenderText(context, plainText, (currentIndex - startIndex) * charWidth + xOffset, yOffset,
                    _context.Foreground, charWidth);
                currentIndex = token.StartColumn;
            }

            var tokenStartInView = Math.Max(startIndex, token.StartColumn);
            var tokenEndInView = Math.Min(endIndex, token.StartColumn + token.Length);
            var tokenText = lineText.Substring(tokenStartInView, tokenEndInView - tokenStartInView);

            RenderText(context, tokenText, (tokenStartInView - startIndex) * charWidth + xOffset, yOffset,
                GetBrushForTokenType(token.Type),
                charWidth);
            currentIndex = tokenEndInView;
        }

        if (currentIndex < endIndex)
        {
            var remainingText = lineText.Substring(currentIndex, endIndex - currentIndex);
            RenderText(context, remainingText, (currentIndex - startIndex) * charWidth + xOffset, yOffset,
                _context.Foreground, charWidth);
        }
    }

    private void RenderText(DrawingContext context, string text, double x, double yOffset, IBrush brush,
        double charWidth)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(_context.FontFamily),
            _context.FontSize,
            brush);

        context.DrawText(formattedText, new Point(x, yOffset));
    }

    public async Task UpdateSyntaxHighlightingAsync(string text)
    {
        if (_backgroundHighlightTask != null && !_backgroundHighlightTask.IsCompleted)
            await _backgroundHighlightTask;

        _backgroundHighlightTask = Task.Run(() =>
        {
            using (new PerformanceLogger("RenderManager.UpdateSyntaxHighlightingAsync"))
            {
                var newTokens = _syntaxHighlighter.HighlightSyntax(text);
                var changedLines = new HashSet<int>();

                for (var i = 0; i < Math.Max(_cachedSyntaxTokens.Count, newTokens.Count); i++)
                    if (i >= _cachedSyntaxTokens.Count || i >= newTokens.Count ||
                        !_cachedSyntaxTokens[i].Equals(newTokens[i]))
                        changedLines.Add(i);

                _cachedSyntaxTokens = newTokens;

                foreach (var lineIndex in changedLines)
                    _lineCache.TryRemove(lineIndex, out _);
            }
        });

        await _backgroundHighlightTask;
        _context.ScrollableViewModel?.TextEditorViewModel.OnInvalidateRequired();
    }

    private IBrush GetBrushForTokenType(SyntaxTokenType type)
    {
        return type switch
        {
            SyntaxTokenType.Keyword => _themeService.GetResourceBrush("KeywordColor"),
            SyntaxTokenType.Comment => _themeService.GetResourceBrush("CommentColor"),
            SyntaxTokenType.String => _themeService.GetResourceBrush("StringColor"),
            SyntaxTokenType.Type => _themeService.GetResourceBrush("TypeColor"),
            SyntaxTokenType.Number => _themeService.GetResourceBrush("NumberColor"),
            _ => Brushes.Black
        };
    }

    private void DrawSelection(DrawingContext context, double viewableAreaHeight,
        ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        if (viewModel.SelectionStart == viewModel.SelectionEnd) return;

        var selectionStart = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
        var selectionEnd = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);

        var cursorPosition = viewModel.CursorPosition;

        var startLine = viewModel.TextBuffer.GetLineIndexFromPosition(selectionStart);
        var endLine = viewModel.TextBuffer.GetLineIndexFromPosition(selectionEnd);
        var cursorLine = viewModel.TextBuffer.GetLineIndexFromPosition(cursorPosition);

        var firstVisibleLine = Math.Max(0, (long)(scrollableViewModel.VerticalOffset / _context.LineHeight) - 5);
        var lastVisibleLine = Math.Min(firstVisibleLine + (long)(viewableAreaHeight / _context.LineHeight) + 11,
            viewModel.TextBuffer.LineCount);

        for (var i = Math.Max(startLine, firstVisibleLine); i <= Math.Min(endLine, lastVisibleLine); i++)
        {
            var lineStartOffset = i == startLine ? selectionStart - viewModel.TextBuffer.LineStarts[(int)i] : 0;
            var lineEndOffset = i == endLine
                ? selectionEnd - viewModel.TextBuffer.LineStarts[(int)i]
                : viewModel.TextBuffer.GetVisualLineLength((int)i);

            if (i == cursorLine && cursorPosition == selectionEnd)
                lineEndOffset = Math.Min(lineEndOffset, cursorPosition - viewModel.TextBuffer.LineStarts[(int)i]);

            var xStart = lineStartOffset * viewModel.CharWidth;
            var xEnd = lineEndOffset * viewModel.CharWidth;
            var y = i * _context.LineHeight;

            var actualLineLength = viewModel.TextBuffer.GetVisualLineLength((int)i) * viewModel.CharWidth;

            if (actualLineLength == 0 && i == cursorLine) continue;

            var isLastSelectionLine = i == endLine;

            var selectionWidth = xEnd - xStart;
            if (actualLineLength == 0)
            {
                selectionWidth = viewModel.CharWidth;
                if (!isLastSelectionLine) selectionWidth += _context.SelectionEndPadding;
            }
            else if (xEnd > actualLineLength)
            {
                selectionWidth = Math.Min(selectionWidth, actualLineLength - xStart);
                if (!isLastSelectionLine) selectionWidth += _context.SelectionEndPadding;
            }
            else if (!isLastSelectionLine)
            {
                selectionWidth += 2;
            }

            selectionWidth = Math.Max(selectionWidth, viewModel.CharWidth);

            var selectionRect = new Rect(xStart, y, selectionWidth, _context.LineHeight);
            context.FillRectangle(_context.SelectionBrush, selectionRect);
        }
    }

    private void DrawCursor(DrawingContext context, ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        var cursorLine = viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.CursorPosition);
        var lineStartPosition = viewModel.TextBuffer.LineStarts[(int)cursorLine];
        var cursorColumn = viewModel.CursorPosition - lineStartPosition;

        var cursorXRelative = cursorColumn * viewModel.CharWidth;
        var cursorY = cursorLine * _context.LineHeight;

        if (cursorXRelative >= 0)
            context.DrawLine(
                new Pen(_context.CursorBrush),
                new Point(cursorXRelative, cursorY),
                new Point(cursorXRelative, cursorY + _context.LineHeight)
            );
    }
}