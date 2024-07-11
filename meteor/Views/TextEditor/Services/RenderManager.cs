using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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

public class RenderManager : IDisposable
{
    private readonly TextEditorContext _context;
    private readonly IThemeService _themeService;
    private readonly Lazy<ISyntaxHighlighter> _syntaxHighlighter;
    private readonly ConcurrentDictionary<int, RenderedLine> _lineCache;
    private ImmutableList<SyntaxToken> _cachedSyntaxTokens = ImmutableList<SyntaxToken>.Empty;
    private readonly HashSet<int> _dirtyLines = new();
    private readonly double _verticalTextOffset;
    private string _filePath;
    private CancellationTokenSource _highlightCancellationTokenSource;
    private const int DebounceDelay = 300;
    private readonly ObjectPool<RenderedLine> _renderedLinePool = new(() => new RenderedLine());

    private readonly ConcurrentDictionary<(string Text, double FontSize, IBrush Brush), FormattedText>
        _formattedTextCache = new();

    public RenderManager(TextEditorContext context, IThemeService themeService,
        Func<ISyntaxHighlighter> syntaxHighlighterFactory, string filePath)
    {
        _context = context;
        _themeService = themeService;
        _syntaxHighlighter = new Lazy<ISyntaxHighlighter>(syntaxHighlighterFactory);
        _lineCache = new ConcurrentDictionary<int, RenderedLine>();
        _filePath = filePath;
        _verticalTextOffset = (_context.LineHeight - _context.FontSize) / 3;
    }

    public void UpdateFilePath(string filePath)
    {
        _filePath = filePath;
        if (_context.ScrollableViewModel?.TextEditorViewModel != null)
        {
            var text = _context.ScrollableViewModel.TextEditorViewModel.TextBuffer.GetText(0,
                _context.ScrollableViewModel.TextEditorViewModel.TextBuffer.Length);
            UpdateSyntaxHighlightingAsync(text).AsTask().ConfigureAwait(false);
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
        await UpdateSyntaxHighlightingAsync(initialText);
        _context.ScrollableViewModel?.TextEditorViewModel.OnInvalidateRequired();
    }

    public void MarkLineDirty(int lineIndex)
    {
        _dirtyLines.Add(lineIndex);
    }

    public void Render(DrawingContext context)
    {
        var batchingContext = new BatchingDrawingContext();
        RenderInternal(batchingContext);
        batchingContext.Flush(context);
    }

    private void RenderInternal(BatchingDrawingContext context)
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

        RenderVisibleLines(context, _context.ScrollableViewModel, firstVisibleLine, lastVisibleLine,
            viewableAreaWidth);
        DrawSelection(context, viewableAreaHeight, _context.ScrollableViewModel);
        DrawCursor(context, _context.ScrollableViewModel);
    }

    private void RenderCurrentLine(BatchingDrawingContext context, TextEditorViewModel viewModel,
        double viewableAreaWidth)
    {
        var cursorLine = viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.CursorPosition);
        var y = cursorLine * _context.LineHeight;

        var selectionStartLine = viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.SelectionStart);
        var selectionEndLine = viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.SelectionEnd);

        var totalWidth = Math.Max(viewModel.WindowWidth,
            viewableAreaWidth + _context.ScrollableViewModel.HorizontalOffset);

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

    private void RenderVisibleLines(BatchingDrawingContext context, ScrollableTextEditorViewModel scrollableViewModel,
        int firstVisibleLine, int lastVisibleLine, double viewableAreaWidth)
    {
        var yOffset = firstVisibleLine * _context.LineHeight;
        var viewModel = scrollableViewModel.TextEditorViewModel;

        for (var i = firstVisibleLine; i < lastVisibleLine; i++)
        {
            if (!_lineCache.TryGetValue(i, out var renderedLine) ||
                renderedLine.NeedsUpdate(scrollableViewModel.HorizontalOffset, viewableAreaWidth))
            {
                renderedLine = RenderLine(viewModel, i, viewableAreaWidth);
                _lineCache[i] = renderedLine;
            }

            context.DrawImage(renderedLine.Image,
                new Rect(0, yOffset, renderedLine.Image.Size.Width, _context.LineHeight));
            yOffset += _context.LineHeight;
        }
    }
    
    private RenderedLine RenderLine(TextEditorViewModel viewModel, int lineIndex, double viewableAreaWidth)
    {
        var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
        if (string.IsNullOrEmpty(lineText))
            return _renderedLinePool.Get().Update(
                new WriteableBitmap(new PixelSize(1, (int)_context.LineHeight), new Vector(96, 96)), 0,
                viewableAreaWidth);

        var horizontalOffset = (int)viewModel.ScrollableTextEditorViewModel.HorizontalOffset;
        var startIndex = (int)(horizontalOffset / viewModel.CharWidth);
        startIndex = Math.Min(startIndex, lineText.Length);

        var maxCharsToDisplay = (int)(viewableAreaWidth / viewModel.CharWidth) + 1;
        var visiblePartLength = Math.Max(0, Math.Min(maxCharsToDisplay, lineText.Length - startIndex));

        var pixelSize = new PixelSize((int)viewableAreaWidth, (int)_context.LineHeight);
        var renderTarget = new RenderTargetBitmap(pixelSize, new Vector(96, 96));

        using (var context = renderTarget.CreateDrawingContext())
        {
            context.FillRectangle(Brushes.Transparent, new Rect(0, 0, pixelSize.Width, pixelSize.Height));
            var xOffset = startIndex * viewModel.CharWidth;

            RenderLineWithSyntaxHighlighting(context, lineText.AsSpan(), _cachedSyntaxTokens, lineIndex, startIndex, 0,
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

        return _renderedLinePool.Get().Update(writeableBitmap, horizontalOffset, viewableAreaWidth);
    }

    public void InvalidateLine(int lineIndex)
    {
        if (_lineCache.TryGetValue(lineIndex, out var renderedLine)) renderedLine.Invalidate();
    }

    public void InvalidateLines(int startLine, int endLine)
    {
        for (var i = startLine; i <= endLine; i++) InvalidateLine(i);
        _context.ScrollableViewModel?.TextEditorViewModel.OnInvalidateRequired();
    }

    private void RenderLineWithSyntaxHighlighting(DrawingContext context, ReadOnlySpan<char> lineText,
        ImmutableList<SyntaxToken> allTokens,
        int lineIndex, int startIndex, double yOffset, double charWidth, int visibleLength, double xOffset)
    {
        var currentIndex = startIndex;
        var endIndex = Math.Min(startIndex + visibleLength, lineText.Length);

        var lineTokens = allTokens.Where(t => t.Line == lineIndex).OrderBy(t => t.StartColumn).ToList();

        if (lineTokens.Count == 0)
        {
            var visibleText = lineText.Slice(startIndex, endIndex - startIndex);
            RenderText(context, visibleText.ToString(), xOffset, yOffset, _context.Foreground, charWidth);
            return;
        }

        foreach (var token in lineTokens)
        {
            if (token.StartColumn >= endIndex)
                break;

            if (token.StartColumn + token.Length <= startIndex)
                continue;

            if (currentIndex < token.StartColumn)
            {
                var plainText = lineText.Slice(currentIndex, Math.Min(token.StartColumn, endIndex) - currentIndex);
                RenderText(context, plainText.ToString(), (currentIndex - startIndex) * charWidth + xOffset, yOffset,
                    _context.Foreground, charWidth);
                currentIndex = token.StartColumn;
            }

            var tokenStartInView = Math.Max(startIndex, token.StartColumn);
            var tokenEndInView = Math.Min(endIndex, token.StartColumn + token.Length);
            var tokenText = lineText.Slice(tokenStartInView, tokenEndInView - tokenStartInView);

            var brush = GetBrushForTokenType(token.Type);
            RenderText(context, tokenText.ToString(), (tokenStartInView - startIndex) * charWidth + xOffset, yOffset,
                brush,
                charWidth);
            currentIndex = tokenEndInView;
        }

        if (currentIndex < endIndex)
        {
            var remainingText = lineText.Slice(currentIndex, endIndex - currentIndex);
            RenderText(context, remainingText.ToString(), (currentIndex - startIndex) * charWidth + xOffset, yOffset,
                _context.Foreground, charWidth);
        }
    }

    private void RenderText(DrawingContext context, string text, double x, double yOffset, IBrush brush,
        double charWidth)
    {
        var formattedText = GetOrCreateFormattedText(text, _context.FontSize, brush);
        var adjustedYOffset = yOffset + _verticalTextOffset;
        context.DrawText(formattedText, new Point(x, adjustedYOffset));
    }

    public ValueTask UpdateSyntaxHighlightingAsync(string text, int startLine = 0, int endLine = -1)
    {
        if (string.IsNullOrEmpty(text))
            return default;

        _highlightCancellationTokenSource?.Cancel();
        _highlightCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _highlightCancellationTokenSource.Token;

        return new ValueTask(Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, cancellationToken);

                var lines = text.Split('\n');
                if (endLine == -1 || endLine >= lines.Length) endLine = lines.Length - 1;

                var partialText = string.Join("\n", lines.Skip(startLine).Take(endLine - startLine + 1));
                var newTokens = _syntaxHighlighter.Value.HighlightSyntax(partialText, startLine, endLine, _filePath);

                _cachedSyntaxTokens = _cachedSyntaxTokens
                    .RemoveAll(t => t.Line >= startLine && t.Line <= endLine)
                    .AddRange(newTokens)
                    .OrderBy(t => t.Line)
                    .ThenBy(t => t.StartColumn)
                    .ToImmutableList();

                for (var i = startLine; i <= endLine; i++)
                    MarkLineDirty(i);
                InvalidateLines(startLine, endLine);
                _context.ScrollableViewModel?.TextEditorViewModel.OnInvalidateRequired();
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
        }, cancellationToken));
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
            _ => _context.Foreground
        };
    }

    private void DrawSelection(BatchingDrawingContext context, double viewableAreaHeight,
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

    private void DrawCursor(BatchingDrawingContext context, ScrollableTextEditorViewModel scrollableViewModel)
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

    private RenderedLine GetOrCreateRenderedLine(int lineIndex, Func<int, RenderedLine> factory)
    {
        return _lineCache.GetOrAdd(lineIndex, _ =>
        {
            var renderedLine = _renderedLinePool.Get();
            factory(lineIndex).CopyTo(renderedLine);
            return renderedLine;
        });
    }

    private FormattedText GetOrCreateFormattedText(string text, double fontSize, IBrush brush)
    {
        return _formattedTextCache.GetOrAdd((text, fontSize, brush), key =>
            new FormattedText(key.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(_context.FontFamily), key.FontSize, key.Brush));
    }

    public void Dispose()
    {
        _highlightCancellationTokenSource?.Cancel();
        _highlightCancellationTokenSource?.Dispose();

        foreach (var renderedLine in _lineCache.Values) _renderedLinePool.Return(renderedLine);
        _lineCache.Clear();
    }
}

public class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _objects = new();
    private readonly Func<T> _objectGenerator;

    public ObjectPool(Func<T> objectGenerator)
    {
        _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
    }

    public T Get()
    {
        return _objects.TryTake(out var item) ? item : _objectGenerator();
    }

    public void Return(T item)
    {
        _objects.Add(item);
    }
}