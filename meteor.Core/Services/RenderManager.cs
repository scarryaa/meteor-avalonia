using System.Collections.Concurrent;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Contexts;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Rendering;
using Microsoft.Extensions.Logging;

namespace meteor.Core.Services
{
    public class RenderManager(
        ITextEditorContext context,
        IThemeService themeService,
        Func<ISyntaxHighlighter> syntaxHighlighterFactory,
        ITextMeasurer textMeasurer,
        ILogger<RenderManager> logger)
        : IRenderManager
    {
        private ITextEditorContext _context = context ?? throw new ArgumentNullException(nameof(context));

        private readonly IThemeService _themeService =
            themeService ?? throw new ArgumentNullException(nameof(themeService));

        private readonly Lazy<ISyntaxHighlighter> _syntaxHighlighter = new(syntaxHighlighterFactory ??
                                                                           throw new ArgumentNullException(
                                                                               nameof(syntaxHighlighterFactory)));

        private readonly ConcurrentDictionary<int, IRenderedLine> _lineCache = new();

        private readonly ITextMeasurer _textMeasurer =
            textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));

        private string _filePath = string.Empty;
        private CancellationTokenSource _highlightCancellationTokenSource = new();
        private const int DebounceDelay = 300;
        private readonly ILogger<RenderManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void UpdateFilePath(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            if (_context?.TextEditorViewModel != null)
            {
                var text = _context.TextEditorViewModel.TextBuffer.GetText(0,
                    _context.TextEditorViewModel.TextBuffer.Length);
                _ = UpdateSyntaxHighlightingAsync(text);
            }
        }

        public void AttachToViewModel(ITextEditorViewModel viewModel)
        {
            _context.TextEditorViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public async Task InitializeAsync(string initialText)
        {
            await UpdateSyntaxHighlightingAsync(initialText);
            _context.TextEditorViewModel.OnInvalidateRequired();
        }

        public void Render(IDrawingContext context, double verticalOffset, double viewportHeight)
        {
            RenderBackground(context, viewportHeight);
            RenderVisibleLines(context, verticalOffset, viewportHeight);
            RenderSelection(context, verticalOffset, viewportHeight);
            RenderCursor(context);
        }

        public void UpdateContext(ITextEditorContext newContext)
        {
            _context = newContext ?? throw new ArgumentNullException(nameof(newContext));
            InvalidateLines(0, _context.TextEditorViewModel.TextBuffer.LineCount - 1);
            _ = UpdateSyntaxHighlightingAsync(
                _context.TextEditorViewModel.TextBuffer.GetText(0, _context.TextEditorViewModel.TextBuffer.Length));
        }

        private void RenderBackground(IDrawingContext context, double viewportHeight)
        {
            var rect = new Rect(0, 0, _context.TextEditorViewModel.RequiredWidth, viewportHeight);
            context.FillRectangle(_context.BackgroundBrush, rect);
        }

        private void RenderVisibleLines(IDrawingContext context, double verticalOffset, double viewportHeight)
        {
            var firstVisibleLine = (int)(verticalOffset / _context.LineHeight);
            var visibleLineCount = (int)(viewportHeight / _context.LineHeight) + 1;
            var lastVisibleLine = Math.Min(firstVisibleLine + visibleLineCount,
                _context.TextEditorViewModel.TextBuffer.LineCount - 1);
            _logger.LogDebug(
                $"verticalOffset: {verticalOffset}, viewportHeight: {viewportHeight}, firstVisibleLine: {firstVisibleLine}, lastVisibleLine: {lastVisibleLine}");
            for (var i = firstVisibleLine; i <= lastVisibleLine; i++) RenderLine(context, i);
        }

        private void RenderLine(IDrawingContext context, int lineIndex)
        {
            var viewModel = _context.TextEditorViewModel;
            var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
            var y = lineIndex * _context.LineHeight;
            var lineRect = new Rect(0, y, viewModel.RequiredWidth, _context.LineHeight);
            context.FillRectangle(_context.BackgroundBrush, lineRect);
            if (!string.IsNullOrEmpty(lineText))
            {
                var formattedText = new FormattedText(
                    lineText,
                    _context.FontFamily.Name,
                    _context.FontStyle,
                    _context.FontWeight,
                    _context.FontSize,
                    _context.ForegroundBrush
                );
                context.DrawText(formattedText, new Point(0, y));
            }
        }

        private void RenderSelection(IDrawingContext context, double verticalOffset, double viewportHeight)
        {
            var viewModel = _context.TextEditorViewModel;
            var (selectionStart, selectionEnd) = GetClampedSelection(viewModel);
            var (startLine, endLine) = GetSelectionLines(viewModel, selectionStart, selectionEnd);

            // Calculate visible lines
            var firstVisibleLine = (int)(verticalOffset / _context.LineHeight);
            var lastVisibleLine = (int)((verticalOffset + viewportHeight) / _context.LineHeight);

            // Clamp to visible range
            var visibleStartLine = Math.Max(startLine, firstVisibleLine);
            var visibleEndLine = Math.Min(endLine, lastVisibleLine);

            for (var i = visibleStartLine; i <= visibleEndLine; i++) RenderSelectionForLine(context, viewModel, i, selectionStart, selectionEnd);
        }

        private void RenderSelectionForLine(IDrawingContext context, ITextEditorViewModel viewModel, int lineIndex,
            int selectionStart, int selectionEnd)
        {
            var (lineStartOffset, lineEndOffset) = GetLineOffsets(viewModel, lineIndex, selectionStart, selectionEnd);
            var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
            var (xStart, xEnd) = MeasureSelection(lineText, lineStartOffset, lineEndOffset);
            var y = lineIndex * _context.LineHeight;
            var selectionRect = new Rect(xStart, y, xEnd - xStart, _context.LineHeight);
            context.FillRectangle(_context.SelectionBrush, selectionRect);
        }

        private (int start, int end) GetClampedSelection(ITextEditorViewModel viewModel)
        {
            var start = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            return (
                Math.Clamp(start, 0, viewModel.TextBuffer.Length),
                Math.Clamp(end, 0, viewModel.TextBuffer.Length)
            );
        }

        private (int startLine, int endLine) GetSelectionLines(ITextEditorViewModel viewModel, int selectionStart,
            int selectionEnd)
        {
            return (
                viewModel.TextBuffer.GetLineIndexFromPosition(selectionStart),
                viewModel.TextBuffer.GetLineIndexFromPosition(selectionEnd)
            );
        }

        private (int startOffset, int endOffset) GetLineOffsets(ITextEditorViewModel viewModel, int lineIndex,
            int selectionStart, int selectionEnd)
        {
            var lineStartPosition = viewModel.TextBuffer.GetLineStartPosition(lineIndex);
            return (
                Math.Max(0, selectionStart - lineStartPosition),
                Math.Min(viewModel.TextBuffer.GetLineLength(lineIndex), selectionEnd - lineStartPosition)
            );
        }

        public (double xStart, double xEnd) MeasureSelection(string lineText, int startOffset, int endOffset)
        {
            if (string.IsNullOrEmpty(lineText)) return (0, 0);

            return (
                _textMeasurer.MeasureWidth(lineText.Substring(0, Math.Min(startOffset, lineText.Length)),
                    _context.FontSize,
                    _context.FontFamily.Name),
                _textMeasurer.MeasureWidth(lineText.Substring(0, Math.Min(endOffset, lineText.Length)),
                    _context.FontSize,
                    _context.FontFamily.Name)
            );
        }

        private void RenderCursor(IDrawingContext context)
        {
            var viewModel = _context.TextEditorViewModel;
            var cursorLine = viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.CursorPosition);
            var cursorColumn = viewModel.CursorPosition - viewModel.TextBuffer.GetLineStartPosition(cursorLine);
            var lineText = viewModel.TextBuffer.GetLineText(cursorLine);
            var x = _textMeasurer.MeasureWidth(lineText.Substring(0, cursorColumn), _context.FontSize,
                _context.FontFamily.Name);
            var y = cursorLine * _context.LineHeight;
            var cursorPen = new Pen(_context.CursorBrush, 1);
            context.DrawLine(cursorPen, new Point(x, y), new Point(x, y + _context.LineHeight));
        }

        public void InvalidateLine(int lineIndex)
        {
            _lineCache.TryRemove(lineIndex, out _);
        }

        public void InvalidateLines(int startLine, int endLine)
        {
            for (var i = startLine; i <= endLine; i++) InvalidateLine(i);
            _context.TextEditorViewModel.OnInvalidateRequired();
        }

        public ValueTask UpdateSyntaxHighlightingAsync(string text, int startLine = 0, int endLine = -1)
        {
            _highlightCancellationTokenSource.Cancel();
            _highlightCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _highlightCancellationTokenSource.Token;
            return new ValueTask(Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DebounceDelay, cancellationToken);
                    var tokens = _syntaxHighlighter.Value.HighlightSyntax(text, startLine, endLine, _filePath);
                    InvalidateLines(startLine, endLine);
                }
                catch (OperationCanceledException)
                {
                    // Ignore cancellation
                }
            }, cancellationToken));
        }

        void IDisposable.Dispose()
        {
            _highlightCancellationTokenSource.Cancel();
            _highlightCancellationTokenSource.Dispose();
            _lineCache.Clear();
        }
    }
}