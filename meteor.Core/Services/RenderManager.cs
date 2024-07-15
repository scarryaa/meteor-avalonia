using System.Collections.Concurrent;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Contexts;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Rendering;

namespace meteor.Core.Services;

public class RenderManager : IRenderManager
{
    private readonly ITextEditorContext _context;
    private readonly IThemeService _themeService;
    private readonly Lazy<ISyntaxHighlighter> _syntaxHighlighter;
    private readonly ConcurrentDictionary<int, IRenderedLine> _lineCache;
    private readonly HashSet<int> _dirtyLines = new();
    private string _filePath;
    private CancellationTokenSource _highlightCancellationTokenSource;
    private const int DebounceDelay = 300;

    public RenderManager(ITextEditorContext context, IThemeService themeService,
        Func<ISyntaxHighlighter> syntaxHighlighterFactory)
    {
        _context = context;
        _themeService = themeService;
        _syntaxHighlighter = new Lazy<ISyntaxHighlighter>(syntaxHighlighterFactory);
        _lineCache = new ConcurrentDictionary<int, IRenderedLine>();
        _filePath = string.Empty;
        _highlightCancellationTokenSource = new CancellationTokenSource();
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

    public void AttachToViewModel(IScrollableTextEditorViewModel viewModel)
    {
        _context.ScrollableViewModel = viewModel;
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

    public void Render(IDrawingContext context)
    {
        RenderBackground(context);
        RenderVisibleLines(context);
        RenderSelection(context);
        RenderCursor(context);
    }

    private void RenderBackground(IDrawingContext context)
    {
        var rect = new Rect(0, 0, _context.ScrollableViewModel.ViewportWidth,
            _context.ScrollableViewModel.ViewportHeight);
        context.FillRectangle(_context.BackgroundBrush, rect);
    }

    private void RenderVisibleLines(IDrawingContext context)
    {
        var firstVisibleLine = (int)(_context.ScrollableViewModel.VerticalOffset / _context.LineHeight);
        var lastVisibleLine = Math.Min(
            firstVisibleLine + (int)(_context.ScrollableViewModel.ViewportHeight / _context.LineHeight) + 1,
            _context.ScrollableViewModel.TextEditorViewModel.TextBuffer.LineCount - 1);

        for (var i = firstVisibleLine; i <= lastVisibleLine; i++) RenderLine(context, i);
    }

    private void RenderLine(IDrawingContext context, int lineIndex)
    {
        var viewModel = _context.ScrollableViewModel.TextEditorViewModel;
        var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
        var y = lineIndex * _context.LineHeight;

        // Render background
        var lineRect = new Rect(0, y, _context.ScrollableViewModel.ViewportWidth, _context.LineHeight);
        context.FillRectangle(_context.BackgroundBrush, lineRect);

        // Render text
        Console.WriteLine($"Rendering line {lineIndex}: {lineText}");
        if (!string.IsNullOrEmpty(lineText))
        {
            var formattedText = new FormattedText(
                lineText,
                _context.FontFamily.ToString(),
                _context.FontStyle,
                _context.FontWeight,
                _context.FontSize,
                _context.ForegroundBrush
            );
            context.DrawText(formattedText, new Point(0, y));
        }
    }

    private void RenderSelection(IDrawingContext context)
    {
        var viewModel = _context.ScrollableViewModel.TextEditorViewModel;
        if (viewModel.SelectionStart == viewModel.SelectionEnd) return;

        var selectionStart = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
        var selectionEnd = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);

        var startLine = viewModel.TextBuffer.GetLineIndexFromPosition(selectionStart);
        var endLine = viewModel.TextBuffer.GetLineIndexFromPosition(selectionEnd);

        for (var i = startLine; i <= endLine; i++)
        {
            var lineStartOffset = i == startLine ? selectionStart - viewModel.TextBuffer.GetLineStartPosition(i) : 0;
            var lineEndOffset = i == endLine
                ? selectionEnd - viewModel.TextBuffer.GetLineStartPosition(i)
                : viewModel.TextBuffer.GetLineLength(i);

            var xStart = lineStartOffset * viewModel.CharWidth;
            var xEnd = lineEndOffset * viewModel.CharWidth;
            var y = i * _context.LineHeight;

            var selectionRect = new Rect(xStart, y, xEnd - xStart, _context.LineHeight);
            context.FillRectangle(_context.SelectionBrush, selectionRect);
        }
    }

    private void RenderCursor(IDrawingContext context)
    {
        var viewModel = _context.ScrollableViewModel.TextEditorViewModel;
        var cursorLine = viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.CursorPosition);
        var cursorColumn = viewModel.CursorPosition - viewModel.TextBuffer.GetLineStartPosition(cursorLine);

        var x = cursorColumn * viewModel.CharWidth;
        var y = cursorLine * _context.LineHeight;

        var cursorPen = new Pen(_context.CursorBrush, 1);

        context.DrawLine(
            cursorPen,
            new Point(x, y),
            new Point(x, y + _context.LineHeight)
        );
    }

    public void InvalidateLine(int lineIndex)
    {
        _lineCache.TryRemove(lineIndex, out _);
        MarkLineDirty(lineIndex);
    }

    public void InvalidateLines(int startLine, int endLine)
    {
        for (var i = startLine; i <= endLine; i++) InvalidateLine(i);
        _context.ScrollableViewModel?.TextEditorViewModel.OnInvalidateRequired();
    }

    public ValueTask UpdateSyntaxHighlightingAsync(string text, int startLine = 0, int endLine = -1)
    {
        _highlightCancellationTokenSource?.Cancel();
        _highlightCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _highlightCancellationTokenSource.Token;

        return new ValueTask(Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, cancellationToken);
                var tokens = _syntaxHighlighter.Value.HighlightSyntax(text, startLine, endLine, _filePath);
                // Update syntax highlighting cache and invalidate affected lines
                InvalidateLines(startLine, endLine);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
        }, cancellationToken));
    }

    public void Dispose()
    {
        _highlightCancellationTokenSource?.Cancel();
        _highlightCancellationTokenSource?.Dispose();
        _lineCache.Clear();
    }
}