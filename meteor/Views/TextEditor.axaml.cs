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
    private double CharWidth { get; }
    private readonly string _fontFamily = "Monospace";
    private readonly List<int> _lineStarts = new();
    private int _cachedLineCount;

    public TextEditor()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        // Measure text
        var referenceText = new FormattedText("0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(_fontFamily), 14, Brushes.Black);
        CharWidth = referenceText.Width;

        Focusable = true;
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
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
        {
            UpdateLineCache();
            Dispatcher.UIThread.Post(InvalidateVisual);
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
            if (!string.IsNullOrEmpty(e.Text)) viewModel.InsertText(e.Text);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
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
    }

    private void HandleReturn(TextEditorViewModel viewModel)
    {
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
        var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var lineStart = _lineStarts[lineIndex];
        _desiredColumn = viewModel.CursorPosition - lineStart;
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

        context.FillRectangle(Brushes.LightGray, new Rect(Bounds.Size));

        var lineCount = GetLineCount();
        if (lineCount == 0) return;

        var viewableAreaWidth = scrollableViewModel.Viewport.Width;
        var viewableAreaHeight = scrollableViewModel.Viewport.Height;

        var firstVisibleLine = Math.Max(0, (int)(scrollableViewModel.VerticalOffset / LineHeight));
        var lastVisibleLine = Math.Min(
            firstVisibleLine + (int)(viewableAreaHeight / LineHeight) + 5,
            lineCount);

        RenderVisibleLines(context, scrollableViewModel, firstVisibleLine, lastVisibleLine, viewableAreaWidth);
        DrawSelection(context, viewableAreaWidth, viewableAreaHeight, scrollableViewModel);
        DrawCursor(context, viewableAreaWidth, viewableAreaHeight, scrollableViewModel);
    }

    private void RenderVisibleLines(DrawingContext context, ScrollableTextEditorViewModel scrollableViewModel,
        int firstVisibleLine, int lastVisibleLine, double viewableAreaWidth)
    {
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
    }

    private string GetVisiblePart(string lineText, double xOffset, double viewableAreaWidth)
    {
        var startIndex = Math.Max(0, (int)(xOffset / CharWidth));
        var endIndex = Math.Min(lineText.Length, startIndex + (int)(viewableAreaWidth / CharWidth));
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
            var xOffsetStart = lineStart * CharWidth - scrollableViewModel.HorizontalOffset;
            var xOffsetEnd = lineEnd * CharWidth - scrollableViewModel.HorizontalOffset;

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
        var cursorX = cursorColumn * CharWidth - scrollableViewModel.HorizontalOffset;
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