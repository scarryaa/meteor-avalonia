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
        // TODO Implement line rendering logic here
    }

    private void RenderSelection(IDrawingContext context)
    {
        // TODO Implement selection rendering logic here
    }

    private void RenderCursor(IDrawingContext context)
    {
        // TODO Implement cursor rendering logic here
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