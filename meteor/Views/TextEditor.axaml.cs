using System;
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
    private const double LineHeight = 20;
    private double TextWidth { get; }
    private readonly string _fontFamily = "Monospace";

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
                        viewModel.CursorPosition--;
                    viewModel.ClearSelection();
                    break;
                case Key.Right:
                    if (viewModel.CursorPosition < viewModel.Rope.Length)
                        viewModel.CursorPosition++;
                    viewModel.ClearSelection();
                    break;
            }

            viewModel.CursorPosition = Math.Max(0, Math.Min(viewModel.CursorPosition, viewModel.Rope.Length));
        }
    }
    
    public override void Render(DrawingContext context)
    {
        if (DataContext is not ScrollableTextEditorViewModel scrollableViewModel) return;
        var viewModel = scrollableViewModel.TextEditorViewModel;

        context.FillRectangle(Brushes.LightGray, new Rect(Bounds.Size));

        var text = viewModel.Rope.GetText();
        var lines = text.Split('\n');

        var viewableAreaWidth = Bounds.Width;
        var viewableAreaHeight = Bounds.Height;

        // Calculate the first and last visible lines
        var firstVisibleLine = Math.Max(0, (int)(scrollableViewModel.VerticalOffset / LineHeight));
        var lastVisibleLine = Math.Min(
            firstVisibleLine + (int)(viewableAreaHeight / LineHeight) + 5,
            lines.Length);

        var yOffset = firstVisibleLine * LineHeight - scrollableViewModel.VerticalOffset % LineHeight;
        
        for (var i = firstVisibleLine; i < lastVisibleLine; i++)
        {
            var lineText = viewModel.Rope.GetLineText(i);
            var xOffset = -scrollableViewModel.HorizontalOffset;

            // Skip drawing if text is completely out of view vertically
            if (yOffset + LineHeight < 0 || yOffset > viewableAreaHeight)
            {
                yOffset += LineHeight;
                continue;
            }

            // Render only the visible part of the line
            var visiblePart = GetVisiblePart(lineText, xOffset, viewableAreaWidth);

            var formattedText = new FormattedText(
                visiblePart,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(_fontFamily),
                14,
                Brushes.Black);

            // Draw only the visible part of the text
            var clipRect = new Rect(0, Math.Max(0, yOffset), viewableAreaWidth, LineHeight);
            using (context.PushClip(clipRect))
            {
                context.DrawText(formattedText, new Point(xOffset, yOffset));
            }

            yOffset += LineHeight;
        }

        // Draw selection
        if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
            DrawSelection(context, text, lines, viewableAreaWidth, viewableAreaHeight, scrollableViewModel);

        // Draw cursor
        DrawCursor(context, text, viewableAreaWidth, viewableAreaHeight, scrollableViewModel);
    }

    private string GetVisiblePart(string lineText, double xOffset, double viewableAreaWidth)
    {
        var startIndex = Math.Max(0, (int)(xOffset / TextWidth));
        var endIndex = Math.Min(lineText.Length, startIndex + (int)(viewableAreaWidth / TextWidth));
        return lineText.Substring(startIndex, endIndex - startIndex);
    }

    private void DrawSelection(DrawingContext context, string text, string[] lines, double viewableAreaWidth,
        double viewableAreaHeight, ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        var selectionStart = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
        var selectionEnd = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
        var selectionText = text.Substring(selectionStart, selectionEnd - selectionStart);
        var selectionLines = selectionText.Split('\n');
        var startLine = text.Substring(0, selectionStart).Split('\n').Length - 1;
        var endLine = startLine + selectionLines.Length - 1;

        var yOffset = startLine * LineHeight - scrollableViewModel.VerticalOffset;
        for (var i = startLine; i <= endLine; i++)
        {
            // Skip if the selection line is not visible
            if (yOffset + LineHeight < 0 || yOffset > viewableAreaHeight)
            {
                yOffset += LineHeight;
                continue;
            }

            var lineStart = i == startLine
                ? selectionStart - text.Substring(0, selectionStart).LastIndexOf('\n') - 1
                : 0;
            var lineEnd = i == endLine
                ? selectionEnd - text.Substring(0, selectionStart + selectionText.Length).LastIndexOf('\n') - 1
                : lines[i].Length;
            var xOffsetStart = lineStart * TextWidth - scrollableViewModel.HorizontalOffset;
            var xOffsetEnd = lineEnd * TextWidth - scrollableViewModel.HorizontalOffset;

            var selectionRect = new Rect(xOffsetStart, yOffset, xOffsetEnd - xOffsetStart, LineHeight);
            var visibleSelectionRect = selectionRect.Intersect(new Rect(0, 0, viewableAreaWidth, viewableAreaHeight));

            if (visibleSelectionRect is { Width: > 0, Height: > 0 })
                context.FillRectangle(new SolidColorBrush(Color.FromArgb(77, 0, 0, 255)), visibleSelectionRect);

            yOffset += LineHeight;
        }
    }

    private void DrawCursor(DrawingContext context, string text, double viewableAreaWidth, double viewableAreaHeight,
        ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        var cursorLine = text.Substring(0, viewModel.CursorPosition).Split('\n').Length - 1;
        var cursorColumn = viewModel.CursorPosition - text.Substring(0, viewModel.CursorPosition).LastIndexOf('\n') - 1;
        var cursorX = cursorColumn * TextWidth - scrollableViewModel.HorizontalOffset;
        var cursorY = cursorLine * LineHeight - scrollableViewModel.VerticalOffset;

        if (cursorX >= 0 && cursorX < viewableAreaWidth && cursorY >= 0 && cursorY < viewableAreaHeight)
            context.DrawLine(new Pen(Brushes.Black), new Point(cursorX, cursorY),
                new Point(cursorX, cursorY + LineHeight));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
            var point = e.GetCurrentPoint(this);
            var column = (int)(point.Position.X / TextWidth);
            var line = (int)(point.Position.Y / LineHeight);

            viewModel.CursorPosition = GetPositionFromLineColumn(line, column, viewModel);
            viewModel.CursorPosition = Math.Min(viewModel.CursorPosition, viewModel.Rope.Length);
            viewModel.SelectionStart = viewModel.CursorPosition;
            viewModel.IsSelecting = true;

            InvalidateVisual();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
            if (viewModel.IsSelecting)
            {
                var point = e.GetCurrentPoint(this);
                var column = (int)(point.Position.X / TextWidth);
                var line = (int)(point.Position.Y / LineHeight);

                viewModel.CursorPosition = GetPositionFromLineColumn(line, column, viewModel);
                viewModel.CursorPosition = Math.Min(viewModel.CursorPosition, viewModel.Rope.Length);
                viewModel.SelectionEnd = viewModel.CursorPosition;

                InvalidateVisual();
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            var viewModel = scrollableViewModel.TextEditorViewModel;
            viewModel.IsSelecting = false;
        }
    }

    private int GetPositionFromLineColumn(int line, int column, TextEditorViewModel viewModel)
    {
        var lines = viewModel.Rope.GetText().Split('\n');

        // Clamp the line parameter to the valid range
        line = Math.Max(0, Math.Min(line, lines.Length - 1));

        // Clamp the column parameter to the valid range
        column = Math.Max(0, Math.Min(column, lines[line].Length));

        var position = 0;

        // +1 for newline character
        for (var i = 0; i < line; i++) position += lines[i].Length + 1;

        position += column;

        // Ensure the position does not exceed the text length
        position = Math.Min(position, viewModel.Rope.Length);

        return position;
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