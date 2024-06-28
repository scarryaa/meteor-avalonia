using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.ViewModels;

namespace meteor.Views;

public partial class TextEditor : UserControl
{
    private const double SelectionEndPadding = 2;
    private const double LinePadding = 20;
    private int _desiredColumn;
    private const double LineHeight = 20;
    private double CharWidth { get; }
    private readonly string _fontFamily = "Monospace";
    private readonly List<int> _lineStarts = new();
    private int _cachedLineCount;
    private readonly Dictionary<int, int> _lineLengths = new();
    private int _longestLineIndex = -1;
    private int _longestLineLength;
    private ScrollableTextEditorViewModel _scrollableViewModel;

    public TextEditor()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        // Measure text to calculate CharWidth
        var referenceText = new FormattedText("0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(_fontFamily), 14, Brushes.Black);
        CharWidth = referenceText.Width;

        Focusable = true;
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            _scrollableViewModel = scrollableViewModel;
            var viewModel = scrollableViewModel.TextEditorViewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateLineCache();
        }
    }

    private void UpdateLineCache()
    {
        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            _lineStarts.Clear();
            _lineLengths.Clear();
            _lineStarts.Add(0);

            var lineStart = 0;
            while (lineStart < viewModel.Rope.Length)
            {
                var nextNewline = viewModel.Rope.IndexOf('\n', lineStart);
                if (nextNewline == -1)
                {
                    _lineLengths[_lineStarts.Count - 1] = viewModel.Rope.Length - lineStart;
                    break;
                }

                _lineStarts.Add(nextNewline + 1);
                _lineLengths[_lineStarts.Count - 2] = nextNewline - lineStart;
                lineStart = nextNewline + 1;
            }

            _cachedLineCount = _lineStarts.Count;

            _longestLineIndex = -1;
            _longestLineLength = 0;


            for (var i = 0; i < _cachedLineCount; i++)
            {
                var length = GetVisualLineLength(viewModel, i);
                _lineLengths[i] = length;

                if (length > _longestLineLength)
                {
                    _longestLineLength = length;
                    _longestLineIndex = i;
                }
            }

            // Update the longest line width property in the ScrollableTextEditorViewModel
            _scrollableViewModel.LongestLineWidth = _longestLineLength * CharWidth + LinePadding;
        }
    }

    private int GetLineLength(TextEditorViewModel viewModel, int lineIndex)
    {
        return _lineLengths.TryGetValue(lineIndex, out var length) ? length : 0;
    }

    private int GetLineCount()
    {
        return _cachedLineCount;
    }

    private string GetLongestLine(TextEditorViewModel viewModel)
    {
        return _longestLineIndex != -1 ? GetLineText(_longestLineIndex) : string.Empty;
    }

    private void PrintLongestLine()
    {
        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var longestLine = GetLongestLine(viewModel);
        }
    }

    public string GetLineText(int lineIndex)
    {
        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            if (lineIndex < 0 || lineIndex >= viewModel.Rope.GetLineCount())
                return string.Empty; // Return empty string if line index is out of range

            return viewModel.Rope.GetLineText(lineIndex);
        }

        return string.Empty;
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TextEditorViewModel.Rope))
        {
            UpdateLineCache();
            Dispatcher.UIThread.Post(InvalidateVisual);
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            if (!string.IsNullOrEmpty(e.Text))
            {
                var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
                viewModel.InsertText(e.Text);
                OnTextInserted(lineIndex, e.Text.Length);
            }
        }
    }

    private void OnTextInserted(int lineIndex, int length)
    {
        if (_lineLengths.ContainsKey(lineIndex))
        {
            var newLength = GetVisualLineLength(_scrollableViewModel.TextEditorViewModel, lineIndex);
            _lineLengths[lineIndex] = newLength;
            if (newLength > _longestLineLength)
            {
                _longestLineLength = newLength;
                _longestLineIndex = lineIndex;
                _scrollableViewModel.LongestLineWidth = _longestLineLength * CharWidth + LinePadding;
            }
        }
        else
        {
            UpdateLineCache();
        }
    }

    private void OnTextDeleted(int lineIndex, int length)
    {
        if (_lineLengths.ContainsKey(lineIndex))
        {
            _lineLengths[lineIndex] -= length;
            if (lineIndex == _longestLineIndex && _lineLengths[lineIndex] < _longestLineLength)
            {
                // Recalculate the longest line if the current longest line is affected
                _longestLineIndex = -1;
                _longestLineLength = 0;
                foreach (var kvp in _lineLengths)
                    if (kvp.Value > _longestLineLength)
                    {
                        _longestLineLength = kvp.Value;
                        _longestLineIndex = kvp.Key;
                    }

                _scrollableViewModel.LongestLineWidth = _longestLineLength * CharWidth;
            }
        }
        else
        {
            UpdateLineCache(); // Full cache update if the line index is not found
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            HandleKeyDown(e, viewModel);
            InvalidateVisual();
        }
    }

    private void HandleKeyDown(KeyEventArgs e, TextEditorViewModel viewModel)
    {
        var ctrlFlag = (e.KeyModifiers & KeyModifiers.Control) != 0;

        if (ctrlFlag)
        {
            HandleControlKeyDown(e, viewModel);
            return;
        }

        switch (e.Key)
        {
            case Key.Return:
                HandleReturn(viewModel);
                break;
            case Key.Back:
                HandleBackspace(viewModel);
                break;
            case Key.Left:
                HandleLeftArrow(viewModel);
                break;
            case Key.Right:
                HandleRightArrow(viewModel);
                break;
            case Key.Up:
                HandleUpArrow(viewModel);
                break;
            case Key.Down:
                HandleDownArrow(viewModel);
                break;
            case Key.Home:
                HandleHome(viewModel);
                break;
            case Key.End:
                HandleEnd(viewModel);
                break;
        }

        viewModel.CursorPosition = Math.Clamp(viewModel.CursorPosition, 0, viewModel.Rope.Length);
    }

    private void HandleEnd(TextEditorViewModel viewModel)
    {
        var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var lineEndPosition = viewModel.Rope.GetLineStartPosition(lineIndex) +
                              GetVisualLineLength(viewModel, lineIndex);
        viewModel.CursorPosition = lineEndPosition;
        UpdateDesiredColumn(viewModel);
        viewModel.ClearSelection();
    }

    private void HandleHome(TextEditorViewModel viewModel)
    {
        var lineStartPosition =
            viewModel.Rope.GetLineStartPosition(GetLineIndex(viewModel, viewModel.CursorPosition));
        viewModel.CursorPosition = lineStartPosition;
        _desiredColumn = 0;
        viewModel.ClearSelection();
    }

    private void HandleRightArrow(TextEditorViewModel viewModel)
    {
        if (viewModel.CursorPosition < viewModel.Rope.Length)
        {
            viewModel.CursorPosition++;
            UpdateDesiredColumn(viewModel);
        }

        viewModel.ClearSelection();
    }

    private void HandleLeftArrow(TextEditorViewModel viewModel)
    {
        if (viewModel.CursorPosition > 0)
        {
            viewModel.CursorPosition--;
            UpdateDesiredColumn(viewModel);
        }

        viewModel.ClearSelection();
    }

    private void HandleBackspace(TextEditorViewModel viewModel)
    {
        if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
        {
            var lineIndex = GetLineIndex(viewModel, viewModel.SelectionStart);
            viewModel.DeleteText(Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd),
                Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart));
            OnTextDeleted(lineIndex, Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart));
            viewModel.CursorPosition = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            viewModel.ClearSelection();
        }
        else if (viewModel.CursorPosition > 0)
        {
            var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
            viewModel.DeleteText(viewModel.CursorPosition - 1, 1);
            OnTextDeleted(lineIndex, 1);
            viewModel.CursorPosition--;
        }
    }

    private void HandleReturn(TextEditorViewModel viewModel)
    {
        if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
        {
            var lineIndex = GetLineIndex(viewModel, viewModel.SelectionStart);
            viewModel.DeleteText(Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd),
                Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart));
            OnTextDeleted(lineIndex, Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart));
            viewModel.CursorPosition = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            viewModel.ClearSelection();
        }

        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        viewModel.InsertText("\n");
        OnTextInserted(currentLineIndex, 1);
        viewModel.CursorPosition++;
    }

    private void HandleControlKeyDown(KeyEventArgs keyEventArgs, TextEditorViewModel viewModel)
    {
        switch (keyEventArgs.Key)
        {
            case Key.A:
                SelectAll();
                break;
            case Key.C:
                CopyText();
                break;
            case Key.V:
                PasteText();
                break;
        }
    }

    private void UpdateDesiredColumn(TextEditorViewModel viewModel)
    {
        // Ensure _lineStarts is correctly populated
        if (_lineStarts.Count == 0) UpdateLineCache();

        var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);

        // Add bounds checking for lineIndex
        if (lineIndex >= 0 && lineIndex < _lineStarts.Count)
        {
            var lineStart = _lineStarts[lineIndex];
            _desiredColumn = viewModel.CursorPosition - lineStart;
        }
        else
        {
            Console.WriteLine(
                $"Error: lineIndex {lineIndex} is out of bounds for _lineStarts with count {_lineStarts.Count}");
            _desiredColumn = 0;
        }
    }


    private void HandleUpArrow(TextEditorViewModel viewModel)
    {
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        if (currentLineIndex > 0)
        {
            var currentLineStart = viewModel.Rope.GetLineStartPosition(currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            // Update desired column only if it's greater than the current column
            _desiredColumn = Math.Max(_desiredColumn, currentColumn);

            var previousLineIndex = currentLineIndex - 1;
            var previousLineStart = viewModel.Rope.GetLineStartPosition(previousLineIndex);
            var previousLineLength = viewModel.Rope.GetLineLength(previousLineIndex);

            // Calculate new cursor position
            viewModel.CursorPosition = previousLineStart + Math.Min(_desiredColumn, previousLineLength - 1);
        }
        else
        {
            // Move to the start of the first line
            viewModel.CursorPosition = 0;
            UpdateDesiredColumn(viewModel);
        }
    }

    private void HandleDownArrow(TextEditorViewModel viewModel)
    {
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        if (currentLineIndex < viewModel.Rope.GetLineCount() - 1)
        {
            var currentLineStart = viewModel.Rope.GetLineStartPosition(currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            // Update the desired column only if it's greater than the current column
            _desiredColumn = Math.Max(_desiredColumn, currentColumn);

            var nextLineIndex = currentLineIndex + 1;
            var nextLineStart = viewModel.Rope.GetLineStartPosition(nextLineIndex);
            var nextLineLength = GetVisualLineLength(viewModel, nextLineIndex);

            // Calculate new cursor position
            viewModel.CursorPosition = nextLineStart + Math.Min(_desiredColumn, nextLineLength);
        }
        else
        {
            // Move to the end of the last line
            var lastLineStart = viewModel.Rope.GetLineStartPosition(currentLineIndex);
            var lastLineLength = viewModel.Rope.GetLineLength(currentLineIndex);
            viewModel.CursorPosition = lastLineStart + lastLineLength;
            UpdateDesiredColumn(viewModel);
        }
    }

    private int GetVisualLineLength(TextEditorViewModel viewModel, int lineIndex)
    {
        var lineLength = GetLineLength(viewModel, lineIndex);
        // Subtract 1 if the line ends with a newline character
        if (lineLength > 0 &&
            viewModel.Rope.GetText(viewModel.Rope.GetLineStartPosition(lineIndex) + lineLength - 1, 1) == "\n")
            lineLength--;
        return lineLength;
    }

    private int GetLineIndex(TextEditorViewModel viewModel, int position)
    {
        if (viewModel.Rope == null) throw new InvalidOperationException("Rope is not initialized in the ViewModel.");

        var lineIndex = 0;
        var accumulatedLength = 0;

        Console.WriteLine($"Calculating line index for position {position}");
        Console.WriteLine($"Total Line Count: {viewModel.Rope.LineCount}");

        while (lineIndex < viewModel.Rope.LineCount &&
               accumulatedLength + viewModel.Rope.GetLineLength(lineIndex) <= position)
        {
            accumulatedLength += viewModel.Rope.GetLineLength(lineIndex);
            lineIndex++;
        }

        // Ensure lineIndex does not exceed the line count
        lineIndex = Math.Max(0, Math.Min(lineIndex, viewModel.Rope.LineCount - 1));

        Console.WriteLine($"Calculated line index: {lineIndex}");
        return lineIndex;
    }


    public override void Render(DrawingContext context)
    {
        if (_scrollableViewModel == null) return;

        context.FillRectangle(Brushes.LightGray, new Rect(Bounds.Size));

        var lineCount = GetLineCount();
        if (lineCount == 0) return;

        var viewableAreaWidth = _scrollableViewModel.Viewport.Width + LinePadding;
        var viewableAreaHeight = _scrollableViewModel.Viewport.Height;

        var firstVisibleLine = Math.Max(0, (int)(_scrollableViewModel.VerticalOffset / LineHeight));
        var lastVisibleLine = Math.Min(
            firstVisibleLine + (int)(viewableAreaHeight / LineHeight) + 5,
            lineCount);

        RenderVisibleLines(context, _scrollableViewModel, firstVisibleLine, lastVisibleLine, viewableAreaWidth);
        DrawSelection(context, viewableAreaWidth, viewableAreaHeight, _scrollableViewModel);
        DrawCursor(context, viewableAreaWidth, viewableAreaHeight, _scrollableViewModel);
    }

    private void RenderVisibleLines(DrawingContext context, ScrollableTextEditorViewModel scrollableViewModel,
        int firstVisibleLine, int lastVisibleLine, double viewableAreaWidth)
    {
        var yOffset = firstVisibleLine * LineHeight;

        for (var i = firstVisibleLine; i < lastVisibleLine; i++)
        {
            var lineText = GetLineText(i);
            var xOffset = 0;

            if (string.IsNullOrEmpty(lineText))
            {
                // Handle empty lines
                yOffset += LineHeight;
                continue;
            }

            // Calculate the start index and the number of characters to display based on the visible area width
            var startIndex = Math.Max(0, (int)(scrollableViewModel.HorizontalOffset / CharWidth));

            // Ensure startIndex is within the lineText length
            if (startIndex >= lineText.Length) startIndex = Math.Max(0, lineText.Length - 1);

            var maxCharsToDisplay = Math.Min(lineText.Length - startIndex,
                (int)((viewableAreaWidth - LinePadding) / CharWidth) + 5);

            // Ensure maxCharsToDisplay is non-negative
            if (maxCharsToDisplay < 0) maxCharsToDisplay = 0;

            // Get the visible part of the line text
            var visiblePart = lineText.Substring(startIndex, maxCharsToDisplay);

            var formattedText = new FormattedText(
                visiblePart,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(_fontFamily),
                14,
                Brushes.Black);

            context.DrawText(formattedText, new Point(xOffset + startIndex * CharWidth, yOffset));

            yOffset += LineHeight;
        }
    }

    private void DrawSelection(DrawingContext context, double viewableAreaWidth,
        double viewableAreaHeight, ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        // Check if there's an actual selection
        if (viewModel.SelectionStart == viewModel.SelectionEnd) return; // No selection to draw

        var selectionStart = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
        var selectionEnd = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
        var cursorPosition = viewModel.CursorPosition;

        var startLine = GetLineIndexFromPosition(selectionStart);
        var endLine = GetLineIndexFromPosition(selectionEnd);
        var cursorLine = GetLineIndexFromPosition(cursorPosition);

        var firstVisibleLine = Math.Max(0, (int)(scrollableViewModel.VerticalOffset / LineHeight));
        var lastVisibleLine = Math.Min(
            firstVisibleLine + (int)(viewableAreaHeight / LineHeight) + 5,
            GetLineCount());

        var yOffset = firstVisibleLine * LineHeight;
        for (var i = firstVisibleLine; i <= lastVisibleLine; i++)
        {
            if (i < startLine || i > endLine) continue;
            if (i > cursorLine) break; // Stop drawing selection after cursor line

            var lineStart = i == startLine ? selectionStart - _lineStarts[i] : 0;
            var lineEnd = i == endLine ? selectionEnd - _lineStarts[i] : GetLineText(i).Length;

            // Adjust lineEnd if cursor is on this line and before the selection end
            if (i == cursorLine && cursorPosition < selectionEnd)
                lineEnd = Math.Min(lineEnd, cursorPosition - _lineStarts[i]);

            // Calculate start and end x offsets considering horizontal scrolling
            var xOffsetStart = Math.Max(0, lineStart * CharWidth);
            var xOffsetEnd = lineEnd * CharWidth + scrollableViewModel.HorizontalOffset;

            // Get the actual line length
            var actualLineLength = GetVisualLineLength(viewModel, i) * CharWidth;

            // Determine if this is the last line of selection
            var isLastSelectionLine = i == endLine || (i == cursorLine && cursorPosition < selectionEnd);

            // Calculate the selection width, accounting for line length, padding, and empty lines
            var selectionWidth = xOffsetEnd - xOffsetStart;
            if (actualLineLength == 0) // Empty line
            {
                selectionWidth = CharWidth;
                if (!isLastSelectionLine) selectionWidth += SelectionEndPadding;
            }
            else if (xOffsetEnd > actualLineLength)
            {
                selectionWidth = Math.Min(selectionWidth, actualLineLength - xOffsetStart);
                if (!isLastSelectionLine) selectionWidth += SelectionEndPadding;
            }
            else if (!isLastSelectionLine)
            {
                selectionWidth += SelectionEndPadding;
            }

            // Ensure minimum width of one character
            selectionWidth = Math.Max(selectionWidth, CharWidth);

            // Adjust for empty new lines at the cursor position
            if (i == cursorLine && actualLineLength == 0)
                selectionWidth = 0; // Only cover the cursor caret on empty new line

            var selectionRect = new Rect(xOffsetStart, yOffset, selectionWidth, LineHeight);

            context.FillRectangle(new SolidColorBrush(Color.FromArgb(100, 139, 205, 192)), selectionRect);

            yOffset += LineHeight;
        }
    }

    private void DrawCursor(DrawingContext context, double viewableAreaWidth, double viewableAreaHeight,
        ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var cursorColumn = viewModel.CursorPosition - _lineStarts[cursorLine];

        // Calculate x offset considering horizontal scrolling
        var cursorX = Math.Max(0, cursorColumn * CharWidth);
        var cursorY = cursorLine * LineHeight;

        context.DrawLine(new Pen(Brushes.Black), new Point(cursorX, cursorY),
            new Point(cursorX, cursorY + LineHeight));
    }

    private int GetLineIndexFromPosition(int position)
    {
        var index = _lineStarts.BinarySearch(position);
        if (index < 0)
        {
            index = ~index;
            index = Math.Max(0, index - 1);
        }

        return index;
    }

    private void SelectAll()
    {
        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            viewModel.SelectionStart = 0;
            viewModel.SelectionEnd = viewModel.Rope.Length;
            viewModel.CursorPosition = viewModel.Rope.Length;
            InvalidateVisual();
        }
    }

    private void CopyText()
    {
        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            if (viewModel.SelectionStart == -1 || viewModel.SelectionEnd == -1)
                return;

            var selectionStart = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var selectionEnd = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            var selectedText = viewModel.Rope.GetText().Substring(selectionStart, selectionEnd - selectionStart);

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            clipboard?.SetTextAsync(selectedText);
        }
    }

    private async void PasteText()
    {
        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        var text = await clipboard.GetTextAsync();
        if (text == null) return;

        if (!string.IsNullOrEmpty(text))
        {
            if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
            {
                var lineIndex = GetLineIndex(viewModel, viewModel.SelectionStart);
                viewModel.DeleteText(Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd),
                    Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart));
                OnTextDeleted(lineIndex, Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart));
                viewModel.CursorPosition = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
                viewModel.ClearSelection();
            }

            viewModel.InsertText(text);
            OnTextInserted(GetLineIndex(viewModel, viewModel.CursorPosition), text.Length);
            viewModel.CursorPosition += text.Length;
            viewModel.CursorPosition = Math.Min(viewModel.CursorPosition, viewModel.Rope.Length);

            UpdateLineCache(); // Ensure the cache is fully updated after pasting

            InvalidateVisual();
        }
    }

}