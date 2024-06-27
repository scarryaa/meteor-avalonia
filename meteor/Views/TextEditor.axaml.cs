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
    private int _desiredColumn;
    private const double LineHeight = 20;
    private double TextWidth { get; }
    private readonly string _fontFamily = "Monospace";
    private readonly List<int> _lineStarts = new();
    private int _cachedLineCount;

    public TextEditor()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
            {
                var viewModel = scrollableViewModel.TextEditorViewModel;
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        };

        // Measure text
        var referenceText = new FormattedText("0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(_fontFamily), 14, Brushes.Black);
        TextWidth = referenceText.Width;

        Focusable = true;
    }

    private void UpdateLineCache()
    {
        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
            _lineStarts.Clear();
            _lineStarts.Add(0);

            var lineStart = 0;
            while (lineStart < viewModel.Rope.Length)
            {
                var nextNewline = viewModel.Rope.IndexOf('\n', lineStart);
                if (nextNewline == -1)
                    break;
                _lineStarts.Add(nextNewline + 1);
                lineStart = nextNewline + 1;
            }

            _cachedLineCount = _lineStarts.Count;
        }
    }

    private int GetLineCount()
    {
        return _cachedLineCount;
    }

    public string GetLineText(int lineIndex)
    {
        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            if (lineIndex < 0 || lineIndex >= scrollableViewModel.TextEditorViewModel.Rope.GetLineCount())
                return string.Empty; // Return empty string if line index is out of range

            return scrollableViewModel.TextEditorViewModel.Rope.GetLineText(lineIndex);
        }

        return string.Empty;
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TextEditorViewModel.Rope))
            Dispatcher.UIThread.Post(InvalidateVisual);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
            if (string.IsNullOrEmpty(e.Text)) return;
            viewModel.InsertText(e.Text);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
            var ctrlFlag = (e.KeyModifiers & KeyModifiers.Control) != 0;

            if (ctrlFlag)
            {
                switch (e.Key)
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

                return;
            }

            switch (e.Key)
            {
                case Key.Return:
                    if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
                    {
                        viewModel.DeleteText(Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd),
                            Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart));
                        viewModel.CursorPosition = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
                        viewModel.ClearSelection();
                    }

                    viewModel.InsertText("\n");
                    viewModel.CursorPosition++;
                    Console.WriteLine(viewModel.Rope.GetLineCount());
                    break;
                case Key.Back:
                    if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
                    {
                        viewModel.DeleteText(Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd),
                            Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart));
                        viewModel.CursorPosition = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
                        viewModel.ClearSelection();
                    }
                    else if (viewModel.CursorPosition > 0)
                    {
                        viewModel.DeleteText(viewModel.CursorPosition - 1, 1);
                        viewModel.CursorPosition--;
                    }

                    break;
                case Key.Left:
                    if (viewModel.CursorPosition > 0)
                    {
                        viewModel.CursorPosition--;
                        UpdateDesiredColumn(viewModel);
                    }
                    viewModel.ClearSelection();
                    break;
                case Key.Right:
                    if (viewModel.CursorPosition < viewModel.Rope.Length)
                    {
                        viewModel.CursorPosition++;
                        UpdateDesiredColumn(viewModel);
                    }
                    viewModel.ClearSelection();
                    break;
                case Key.Up:
                    MoveCursorUp(viewModel);
                    viewModel.ClearSelection();
                    break;
                case Key.Down:
                    MoveCursorDown(viewModel);
                    viewModel.ClearSelection();
                    break;
                case Key.Home:
                    var lineStartPosition =
                        viewModel.Rope.GetLineStartPosition(GetLineIndex(viewModel, viewModel.CursorPosition));
                    viewModel.CursorPosition = lineStartPosition;
                    _desiredColumn = 0;
                    viewModel.ClearSelection();
                    break;
                case Key.End:
                    var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
                    var lineEndPosition = viewModel.Rope.GetLineStartPosition(lineIndex) +
                                          GetVisualLineLength(viewModel, lineIndex);
                    viewModel.CursorPosition = lineEndPosition;
                    UpdateDesiredColumn(viewModel);
                    viewModel.ClearSelection();
                    break;
            }

            viewModel.CursorPosition = Math.Max(0, Math.Min(viewModel.CursorPosition, viewModel.Rope.Length));
            InvalidateVisual();
        }
    }

    private void UpdateDesiredColumn(TextEditorViewModel viewModel)
    {
        var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var lineStart = viewModel.Rope.GetLineStartPosition(lineIndex);
        _desiredColumn = viewModel.CursorPosition - lineStart;
    }

    private void MoveCursorUp(TextEditorViewModel viewModel)
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

    private void MoveCursorDown(TextEditorViewModel viewModel)
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
        var lineLength = viewModel.Rope.GetLineLength(lineIndex);
        // Subtract 1 if the line ends with a newline character
        if (lineLength > 0 &&
            viewModel.Rope.GetText(viewModel.Rope.GetLineStartPosition(lineIndex) + lineLength - 1, 1) ==
            "\n") lineLength--;
        return lineLength;
    }

    private int GetLineIndex(TextEditorViewModel viewModel, int position)
    {
        var lineIndex = 0;
        var accumulatedLength = 0;

        while (accumulatedLength + viewModel.Rope.GetLineLength(lineIndex) <= position &&
               lineIndex < viewModel.Rope.GetLineCount() - 1)
        {
            accumulatedLength += viewModel.Rope.GetLineLength(lineIndex);
            lineIndex++;
        }

        return lineIndex;
    }

    public override void Render(DrawingContext context)
    {
        if (DataContext is not ScrollableTextEditorViewModel scrollableViewModel) return;

        UpdateLineCache();

        context.FillRectangle(Brushes.LightGray, new Rect(Bounds.Size));

        var lineCount = GetLineCount();
        if (lineCount == 0) return; // Exit if there are no lines

        var viewableAreaWidth = scrollableViewModel.Viewport.Width;
        var viewableAreaHeight = scrollableViewModel.Viewport.Height;

        var firstVisibleLine = Math.Max(0, (int)(scrollableViewModel.VerticalOffset / LineHeight));
        var lastVisibleLine = Math.Min(
            firstVisibleLine + (int)(viewableAreaHeight / LineHeight) + 5,
            lineCount);

        var yOffset = firstVisibleLine * LineHeight;

        for (var i = firstVisibleLine; i < lastVisibleLine; i++)
        {
            var lineText = GetLineText(i);
            var xOffset = -scrollableViewModel.HorizontalOffset;

            var visiblePart = GetVisiblePart(lineText, xOffset, viewableAreaWidth);

            var formattedText = new FormattedText(
                visiblePart,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(_fontFamily),
                14,
                Brushes.Black);

            context.DrawText(formattedText, new Point(xOffset, yOffset));

            yOffset += LineHeight;
        }

        DrawSelection(context, viewableAreaWidth, viewableAreaHeight, scrollableViewModel);
        DrawCursor(context, viewableAreaWidth, viewableAreaHeight, scrollableViewModel);
    }

    private string GetVisiblePart(string lineText, double xOffset, double viewableAreaWidth)
    {
        var startIndex = Math.Max(0, (int)(xOffset / TextWidth));
        var endIndex = Math.Min(lineText.Length, startIndex + (int)(viewableAreaWidth / TextWidth));
        return lineText.Substring(startIndex, endIndex - startIndex);
    }

    private void DrawSelection(DrawingContext context, double viewableAreaWidth,
        double viewableAreaHeight, ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        var selectionStart = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
        var selectionEnd = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);

        var startLine = GetLineIndexFromPosition(selectionStart);
        var endLine = GetLineIndexFromPosition(selectionEnd);

        var firstVisibleLine = Math.Max(0, (int)(scrollableViewModel.VerticalOffset / LineHeight));
        var lastVisibleLine = Math.Min(
            firstVisibleLine + (int)(viewableAreaHeight / LineHeight) + 5,
            GetLineCount());

        var yOffset = firstVisibleLine * LineHeight;
        for (var i = firstVisibleLine; i <= lastVisibleLine; i++)
        {
            if (i < startLine || i > endLine) continue;

            var lineStart = i == startLine ? selectionStart - _lineStarts[i] : 0;
            var lineEnd = i == endLine ? selectionEnd - _lineStarts[i] : GetLineText(i).Length;
            var xOffsetStart = lineStart * TextWidth - scrollableViewModel.HorizontalOffset;
            var xOffsetEnd = lineEnd * TextWidth - scrollableViewModel.HorizontalOffset;

            var selectionRect = new Rect(xOffsetStart, yOffset, xOffsetEnd - xOffsetStart, LineHeight);

            context.FillRectangle(new SolidColorBrush(Color.FromArgb(77, 0, 0, 255)), selectionRect);

            yOffset += LineHeight;
        }
    }

    private void DrawCursor(DrawingContext context, double viewableAreaWidth, double viewableAreaHeight,
        ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var cursorColumn = viewModel.CursorPosition - _lineStarts[cursorLine];
        var cursorX = cursorColumn * TextWidth - scrollableViewModel.HorizontalOffset;
        var cursorY = cursorLine * LineHeight;

        context.DrawLine(new Pen(Brushes.Black), new Point(cursorX, cursorY),
            new Point(cursorX, cursorY + LineHeight));
    }

    private int GetLineIndexFromPosition(int position)
    {
        var low = 0;
        var high = _lineStarts.Count - 1;

        while (low <= high)
        {
            var mid = (low + high) / 2;
            if (_lineStarts[mid] <= position && (mid == _lineStarts.Count - 1 || _lineStarts[mid + 1] > position))
                return mid;
            if (_lineStarts[mid] > position)
                high = mid - 1;
            else
                low = mid + 1;
        }

        return _lineStarts.Count - 1;
    }

    private void SelectAll()
    {
        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
            viewModel.SelectionStart = 0;
            viewModel.SelectionEnd = viewModel.Rope.Length;
            viewModel.CursorPosition = viewModel.Rope.Length;
            InvalidateVisual();
        }
    }

    private void CopyText()
    {
        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
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
        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null) return;

            var text = await clipboard.GetTextAsync();
            if (text == null) return;

            if (!string.IsNullOrEmpty(text))
            {
                if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
                {
                    viewModel.DeleteText(Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd),
                        Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart));
                    viewModel.CursorPosition = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
                    viewModel.ClearSelection();
                }

                viewModel.InsertText(text);
                viewModel.CursorPosition += text.Length;
                viewModel.CursorPosition = Math.Min(viewModel.CursorPosition, viewModel.Rope.Length);

                InvalidateVisual();
            }
        }
    }
}