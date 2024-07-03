using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using meteor.ViewModels;
using meteor.Views.Contexts;

namespace meteor.Views.Services;

public class RenderManager
{
    private readonly TextEditorContext _context;

    public void UpdateContextViewModel(ScrollableTextEditorViewModel viewModel)
    {
        _context.ScrollableViewModel = viewModel;
    }

    public RenderManager(TextEditorContext context)
    {
        _context = context;
    }

    public void AttachToViewModel(ScrollableTextEditorViewModel viewModel)
    {
        viewModel.ParentRenderManager = this;
        UpdateContextViewModel(viewModel);
    }

    public void Render(DrawingContext context)
    {
        var adjustedHeight = _context.ScrollableViewModel.Viewport.Height + _context.ScrollableViewModel.VerticalOffset;
        var adjustedWidth = _context.ScrollableViewModel.Viewport.Width + _context.ScrollableViewModel.HorizontalOffset;
        context.FillRectangle(_context.BackgroundBrush, new Rect(new Size(adjustedWidth, adjustedHeight)));

        var lineCount = _context.ScrollableViewModel.TextEditorViewModel.TextBuffer.LineCount;
        if (lineCount == 0) return;

        var viewableAreaWidth = _context.ScrollableViewModel.Viewport.Width + _context.LinePadding;
        var viewableAreaHeight = _context.ScrollableViewModel.Viewport.Height;

        var firstVisibleLine =
            Math.Max(0, (long)(_context.ScrollableViewModel.VerticalOffset / _context.LineHeight) - 5);
        var lastVisibleLine = Math.Min(firstVisibleLine + (long)(viewableAreaHeight / _context.LineHeight) + 11,
            lineCount);

        var viewModel = _context.ScrollableViewModel.TextEditorViewModel;
        RenderCurrentLine(context, viewModel, viewableAreaWidth);

        RenderVisibleLines(context, _context.ScrollableViewModel, firstVisibleLine, lastVisibleLine, viewableAreaWidth);
        DrawSelection(context, viewableAreaHeight, _context.ScrollableViewModel);
        DrawCursor(context, _context.ScrollableViewModel);
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
        long firstVisibleLine, long lastVisibleLine, double viewableAreaWidth)
    {
        const int startIndexBuffer = 5;
        var yOffset = firstVisibleLine * _context.LineHeight;

        for (var i = firstVisibleLine; i < lastVisibleLine; i++)
        {
            var lineText = scrollableViewModel.TextEditorViewModel.TextBuffer.GetLineText((int)i);
            if (string.IsNullOrEmpty(lineText))
            {
                yOffset += (long)_context.LineHeight;
                continue;
            }

            var startIndex = Math.Max(0,
                (long)(scrollableViewModel.HorizontalOffset / scrollableViewModel.TextEditorViewModel.CharWidth) -
                startIndexBuffer);
            startIndex = Math.Min(startIndex, lineText.Length - 1);

            var maxCharsToDisplay = Math.Min(lineText.Length - startIndex,
                (long)((viewableAreaWidth - _context.LinePadding) / scrollableViewModel.TextEditorViewModel.CharWidth) +
                startIndexBuffer * 2);
            maxCharsToDisplay = Math.Max(0, maxCharsToDisplay);

            var visiblePart = lineText.Substring((int)startIndex, (int)maxCharsToDisplay);

            var formattedText = new FormattedText(
                visiblePart,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(_context.FontFamily),
                _context.FontSize,
                _context.Foreground);

            var verticalOffset = (_context.LineHeight - formattedText.Height) / 2;

            context.DrawText(formattedText,
                new Point(startIndex * scrollableViewModel.TextEditorViewModel.CharWidth,
                    yOffset + verticalOffset));

            yOffset += _context.LineHeight;
        }
    }

    private void DrawSelection(DrawingContext context,
        double viewableAreaHeight, ScrollableTextEditorViewModel scrollableViewModel)
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

    private void DrawCursor(DrawingContext context,
        ScrollableTextEditorViewModel scrollableViewModel)
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