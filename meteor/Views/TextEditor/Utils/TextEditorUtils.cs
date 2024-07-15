using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace meteor.Views.Utils;

public class TextEditorUtils(TextEditorViewModel viewModel)
{
    private TextEditorViewModel _viewModel = viewModel;
    private readonly HashSet<char> _commonCodingSymbols = new("(){}[]<>.,;:'\"\\|`~!@#$%^&*-+=/?");

    public void UpdateViewModel(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void MeasureCharWidth()
    {
        var referenceText = new FormattedText(
            "X",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(_viewModel.FontFamily),
            _viewModel.FontSize,
            Brushes.Black);

        _viewModel.CharWidth = referenceText.Width;
    }

    public Point GetPointFromPosition(long position)
    {
        if (_viewModel == null)
            return new Point(0, 0);

        var lineIndex = GetLineIndex(_viewModel, position);
        var lineStart = _viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var column = position - lineStart;

        var x = column * _viewModel.CharWidth - _viewModel.ScrollableTextEditorViewModel.HorizontalOffset;
        var y = lineIndex * _viewModel.LineHeight - _viewModel.ScrollableTextEditorViewModel.VerticalOffset;

        return new Point(x, y);
    }

    public long GetLineIndex(TextEditorViewModel viewModel, long position)
    {
        if (viewModel?.TextBuffer?.Rope == null)
            throw new ArgumentNullException(nameof(viewModel), "TextEditorViewModel or its properties cannot be null.");
        return viewModel.TextBuffer.Rope.GetLineIndexFromPosition((int)position);
    }

    public long GetPositionFromPoint(Point point)
    {
        if (_viewModel == null)
            return 0;

        var lineIndex = (long)(point.Y / _viewModel.LineHeight);

        // Check if the lineIndex is beyond the last line
        if (lineIndex >= _viewModel.LineCount) return _viewModel.TextBuffer.Length;

        var column = (long)(point.X / _viewModel.CharWidth);

        lineIndex = Math.Max(0, Math.Min(lineIndex, _viewModel.LineCount - 1));
        var lineStart = _viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineLength = _viewModel.TextBuffer.GetVisualLineLength((int)lineIndex);

        // If the click is beyond the end of the line text, set the column to the line length
        column = Math.Max(0, Math.Min(column, lineLength));

        return lineStart + column;
    }

    public (long start, long end) FindWordOrSymbolBoundaries(TextEditorViewModel viewModel, long position)
    {
        var lineIndex = GetLineIndex(viewModel, position);
        var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
        var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var localPos = position - lineStart;

        if (string.IsNullOrEmpty(lineText) || localPos >= lineText.Length)
        {
            // Start from the end of the line and move backwards
            var lastNonWhitespaceIndex = lineText.TrimEnd().Length - 1;
            if (lastNonWhitespaceIndex < 0)
                return (lineStart, lineStart); // Empty line

            localPos = lastNonWhitespaceIndex;
        }

        var start = (int)localPos;
        var end = (int)localPos;

        var isWhitespace = char.IsWhiteSpace(lineText[start]);
        var isSymbol = _commonCodingSymbols.Contains(lineText[start]);

        if (isWhitespace)
        {
            var whitespaceStart = start;
            var whitespaceEnd = start;

            while (whitespaceStart > 0 && char.IsWhiteSpace(lineText[whitespaceStart - 1])) whitespaceStart--;
            while (whitespaceEnd < lineText.Length && char.IsWhiteSpace(lineText[whitespaceEnd])) whitespaceEnd++;

            if (whitespaceEnd - whitespaceStart > 1)
            {
                start = whitespaceStart;
                end = whitespaceEnd;
                return (lineStart + start, lineStart + end);
            }

            // If it's a single whitespace character, fall back to word or symbol selection
            isWhitespace = false;
        }

        if (!isWhitespace && !isSymbol)
        {
            while (start > 0 && !char.IsWhiteSpace(lineText[start - 1]) &&
                   !_commonCodingSymbols.Contains(lineText[start - 1])) start--;
            while (end < lineText.Length && !char.IsWhiteSpace(lineText[end]) &&
                   !_commonCodingSymbols.Contains(lineText[end])) end++;
        }

        if (isSymbol)
            // Ensure only one symbol is selected
            end = start + 1;

        return (lineStart + start, lineStart + end);
    }

    public bool IsCommonCodingSymbol(char c)
    {
        return _commonCodingSymbols.Contains(c);
    }

    public double GetTextWidth(string text)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(_viewModel.FontFamily),
            _viewModel.FontSize,
            Brushes.Black);

        return formattedText.Width;
    }

    public long GetLongestLineLength()
    {
        long maxLength = 0;
        for (var i = 0; i < _viewModel.TextBuffer.LineCount; i++)
        {
            var lineLength = _viewModel.TextBuffer.GetLineLength(i);
            if (lineLength > maxLength) maxLength = lineLength;
        }

        return maxLength;
    }
}