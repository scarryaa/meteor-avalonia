using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
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
    private CancellationTokenSource _highlightCancellationTokenSource;
    private const int DebounceDelay = 300;
    private readonly TextEditorContext _context;
    private readonly IThemeService _themeService;
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly ConcurrentDictionary<int, RenderedLine> _lineCache;
    private List<SyntaxToken> _cachedSyntaxTokens;
    private string _lastProcessedText;
    private Task _backgroundHighlightTask;
    private readonly double _verticalTextOffset;
    private string _filePath;

    public RenderManager(TextEditorContext context, IThemeService themeService, ISyntaxHighlighter syntaxHighlighter,
        string filePath)
    {
        _context = context;
        _themeService = themeService;
        _syntaxHighlighter = syntaxHighlighter;
        _lineCache = new ConcurrentDictionary<int, RenderedLine>();
        _cachedSyntaxTokens = new List<SyntaxToken>();
        _filePath = filePath;

        _verticalTextOffset = (_context.LineHeight - _context.FontSize) / 3;
    }

    public void UpdateFilePath(string filePath)
    {
        _filePath = filePath;
        // Trigger a re-highlight of the entire document
        if (_context.ScrollableViewModel?.TextEditorViewModel != null)
        {
            var text = _context.ScrollableViewModel.TextEditorViewModel.TextBuffer.GetText(0,
                _context.ScrollableViewModel.TextEditorViewModel.TextBuffer.Length);
            UpdateSyntaxHighlightingAsync(text).ConfigureAwait(false);
        }
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
        Console.WriteLine("RenderManager.InitializeAsync called");
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
                (int)lineCount);

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
        Console.WriteLine(
            $"RenderLineWithSyntaxHighlighting: LineIndex: {lineIndex}, StartIndex: {startIndex}, VisibleLength: {visibleLength}");
        Console.WriteLine($"Line Text: {lineText}");

        var currentIndex = startIndex;
        var endIndex = Math.Min(startIndex + visibleLength, lineText.Length);

        var lineTokens = allTokens.Where(t => t.Line == lineIndex).OrderBy(t => t.StartColumn).ToList();
        Console.WriteLine($"Number of tokens for this line: {lineTokens.Count}");

        if (lineTokens.Count == 0)
        {
            Console.WriteLine("No tokens for this line, rendering as plain text");
            var visibleText = lineText.Substring(startIndex, endIndex - startIndex);
            RenderText(context, visibleText, xOffset, yOffset, _context.Foreground, charWidth);
            return;
        }

        foreach (var token in lineTokens)
        {
            Console.WriteLine(
                $"Processing token: Type: {token.Type}, StartColumn: {token.StartColumn}, Length: {token.Length}");

            if (token.StartColumn >= endIndex)
            {
                Console.WriteLine("Token starts after visible part, breaking");
                break;
            }

            if (token.StartColumn + token.Length <= startIndex)
            {
                Console.WriteLine("Token ends before visible part, continuing");
                continue;
            }

            if (currentIndex < token.StartColumn)
            {
                var plainText = lineText.Substring(currentIndex, Math.Min(token.StartColumn, endIndex) - currentIndex);
                Console.WriteLine($"Rendering plain text: {plainText}");
                RenderText(context, plainText, (currentIndex - startIndex) * charWidth + xOffset, yOffset,
                    _context.Foreground, charWidth);
                currentIndex = token.StartColumn;
            }

            var tokenStartInView = Math.Max(startIndex, token.StartColumn);
            var tokenEndInView = Math.Min(endIndex, token.StartColumn + token.Length);
            var tokenText = lineText.Substring(tokenStartInView, tokenEndInView - tokenStartInView);

            Console.WriteLine(
                $"Rendering token: Type: {token.Type}, Text: {tokenText}, Start: {tokenStartInView}, End: {tokenEndInView}");
            var brush = GetBrushForTokenType(token.Type);
            Console.WriteLine($"Token brush: {brush}");
            RenderText(context, tokenText, (tokenStartInView - startIndex) * charWidth + xOffset, yOffset, brush,
                charWidth);
            currentIndex = tokenEndInView;
        }

        if (currentIndex < endIndex)
        {
            var remainingText = lineText.Substring(currentIndex, endIndex - currentIndex);
            Console.WriteLine($"Rendering remaining text: {remainingText}");
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

        // Adjust the yOffset to center the text vertically
        var adjustedYOffset = yOffset + _verticalTextOffset;

        context.DrawText(formattedText, new Point(x, adjustedYOffset));
    }

    public async Task UpdateSyntaxHighlightingAsync(string text, int startLine = 0, int endLine = -1)
    {
        Console.WriteLine(
            $"UpdateSyntaxHighlightingAsync called. StartLine: {startLine}, EndLine: {endLine}, FilePath: {_filePath}");

        // Cancel any ongoing highlight task
        _highlightCancellationTokenSource?.Cancel();
        _highlightCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _highlightCancellationTokenSource.Token;

        try
        {
            // Delay to debounce rapid calls
            await Task.Delay(DebounceDelay, cancellationToken);

            await Task.Run(() =>
            {
                using (new PerformanceLogger("RenderManager.UpdateSyntaxHighlightingAsync"))
                {
                    List<SyntaxToken> newTokens;
                    var lines = text.Split('\n');

                    if (endLine == -1 || endLine >= lines.Length) endLine = lines.Length - 1;

                    // Ensure we're highlighting complete lines
                    var partialText = string.Join("\n", lines.Skip(startLine).Take(endLine - startLine + 1));
                    newTokens = _syntaxHighlighter.HighlightSyntax(partialText, startLine, endLine, _filePath);

                    Console.WriteLine($"Generated {newTokens.Count} new tokens");

                    lock (_cachedSyntaxTokens)
                    {
                        // Remove old tokens for the updated lines
                        _cachedSyntaxTokens.RemoveAll(t => t.Line >= startLine && t.Line <= endLine);
                        // Add new tokens
                        _cachedSyntaxTokens.AddRange(newTokens);
                        // Sort tokens
                        _cachedSyntaxTokens = _cachedSyntaxTokens
                            .OrderBy(t => t.Line)
                            .ThenBy(t => t.StartColumn)
                            .ToList();
                    }

                    // Invalidate changed lines in the cache
                    for (var i = startLine; i <= endLine; i++) _lineCache.TryRemove(i, out _);

                    Console.WriteLine($"Updated syntax highlighting. Lines {startLine} to {endLine}");
                }
            }, cancellationToken);

            Console.WriteLine("Syntax highlighting update completed");
            _context.ScrollableViewModel?.TextEditorViewModel.OnInvalidateRequired();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Syntax highlighting update was cancelled");
        }
    }

    private IBrush GetBrushForTokenType(SyntaxTokenType type)
    {
        IBrush brush;
        switch (type)
        {
            case SyntaxTokenType.Keyword:
                brush = _themeService.GetResourceBrush("KeywordColor");
                break;
            case SyntaxTokenType.Comment:
                brush = _themeService.GetResourceBrush("CommentColor");
                break;
            case SyntaxTokenType.String:
                brush = _themeService.GetResourceBrush("StringColor");
                break;
            case SyntaxTokenType.Type:
                brush = _themeService.GetResourceBrush("TypeColor");
                break;
            case SyntaxTokenType.Number:
                brush = _themeService.GetResourceBrush("NumberColor");
                break;
            default:
                brush = _context.Foreground;
                break;
        }

        Console.WriteLine($"GetBrushForTokenType: TokenType: {type}, Brush: {brush}");
        return brush;
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