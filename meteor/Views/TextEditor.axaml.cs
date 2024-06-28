using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.ViewModels;

namespace meteor.Views;

public partial class TextEditor : UserControl
{
    private bool _suppressScrollOnNextCursorMove;
    private bool _isSelecting;
    private int _selectionAnchor = -1;
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
    private (int start, int end) _lastKnownSelection = (-1, -1);

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
        if (_scrollableViewModel?.TextEditorViewModel != null)
            _scrollableViewModel.TextEditorViewModel.PropertyChanged -= ViewModel_PropertyChanged;

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            _scrollableViewModel = scrollableViewModel;
            var viewModel = scrollableViewModel.TextEditorViewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateLineCache();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (_scrollableViewModel?.TextEditorViewModel != null)
            _scrollableViewModel.TextEditorViewModel.PropertyChanged -= ViewModel_PropertyChanged;
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
        return _lineLengths.GetValueOrDefault(lineIndex, 0);
    }

    private int GetLineCount()
    {
        return _cachedLineCount;
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
        else if (e.PropertyName == nameof(TextEditorViewModel.CursorPosition))
        {
            Dispatcher.UIThread.Post(EnsureCursorVisible);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var position = GetPositionFromPoint(e.GetPosition(this));
            viewModel.CursorPosition = position;
            _selectionAnchor = position;
            UpdateSelection(viewModel);
            _isSelecting = true;
            _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isSelecting && _scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var position = GetPositionFromPoint(e.GetPosition(this));
            viewModel.CursorPosition = position;
            UpdateSelection(viewModel);
            _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        _isSelecting = false;
        e.Handled = true;
    }

    private void UpdateSelection(TextEditorViewModel viewModel)
    {
        if (_selectionAnchor != -1)
        {
            viewModel.SelectionStart = Math.Min(_selectionAnchor, viewModel.CursorPosition);
            viewModel.SelectionEnd = Math.Max(_selectionAnchor, viewModel.CursorPosition);
            _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
        }
    }

    private void EnsureCursorVisible()
    {
        if (_scrollableViewModel == null) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var cursorColumn = viewModel.CursorPosition - _lineStarts[cursorLine];

        if (!_suppressScrollOnNextCursorMove)
        {
            // Vertical scrolling
            var cursorY = cursorLine * LineHeight;

            if (cursorY < _scrollableViewModel.VerticalOffset)
                _scrollableViewModel.VerticalOffset = cursorY;
            else if (cursorY + LineHeight > _scrollableViewModel.VerticalOffset + _scrollableViewModel.Viewport.Height)
                _scrollableViewModel.VerticalOffset = cursorY + LineHeight - _scrollableViewModel.Viewport.Height;

            // Horizontal scrolling
            var cursorX = cursorColumn * CharWidth;
            var viewportWidth = _scrollableViewModel.Viewport.Width;
            var currentOffset = _scrollableViewModel.HorizontalOffset;

            // Define a margin (e.g., 10% of viewport width) to keep the cursor visible
            var margin = viewportWidth * 0.1;

            if (cursorX < currentOffset + margin)
                // Cursor is too close to or beyond the left edge
                _scrollableViewModel.HorizontalOffset = Math.Max(0, cursorX - margin);
            else if (cursorX > currentOffset + viewportWidth - margin)
                // Cursor is too close to or beyond the right edge
                _scrollableViewModel.HorizontalOffset = Math.Max(0, cursorX - viewportWidth + margin);
        }

        _suppressScrollOnNextCursorMove = false;
        InvalidateVisual();
    }

    private int GetPositionFromPoint(Point point)
    {
        if (_scrollableViewModel == null)
            return 0;

        var lineIndex = (int)(point.Y / LineHeight);
        var column = (int)(point.X / CharWidth);

        lineIndex = Math.Max(0, Math.Min(lineIndex, GetLineCount() - 1));
        var lineStart = _lineStarts[lineIndex];
        var lineLength = GetVisualLineLength(_scrollableViewModel.TextEditorViewModel, lineIndex);
        column = Math.Max(0, Math.Min(column, lineLength));

        return lineStart + column;
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            if (!string.IsNullOrEmpty(e.Text))
            {
                var insertPosition = viewModel.CursorPosition;

                // Check if there is a selection
                if (_lastKnownSelection.start != _lastKnownSelection.end)
                {
                    // Handle deletion of selected text
                    var start = Math.Min(_lastKnownSelection.start, _lastKnownSelection.end);
                    var end = Math.Max(_lastKnownSelection.start, _lastKnownSelection.end);
                    var length = end - start;

                    viewModel.DeleteText(start, length);

                    // Update insertPosition to be where the selection started
                    insertPosition = start;
                }

                var currentLineIndex = GetLineIndex(viewModel, insertPosition);
                viewModel.InsertText(insertPosition, e.Text);

                // Update cursor position after insertion
                viewModel.CursorPosition = insertPosition + e.Text.Length;

                // Clear the selection after insertion
                viewModel.SelectionStart = viewModel.CursorPosition;
                viewModel.SelectionEnd = viewModel.CursorPosition;
                viewModel.IsSelecting = false;

                // Update the last known selection
                _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition);
                _selectionAnchor = -1; // Reset selection anchor
            }
        }
        
        InvalidateVisual();
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
            var newLength = GetVisualLineLength(_scrollableViewModel.TextEditorViewModel, lineIndex);
            _lineLengths[lineIndex] = newLength;

            // Recalculate the longest line if necessary
            if (newLength < _longestLineLength && lineIndex == _longestLineIndex)
            {
                _longestLineLength = 0;
                _longestLineIndex = -1;
                foreach (var kvp in _lineLengths)
                    if (kvp.Value > _longestLineLength)
                    {
                        _longestLineLength = kvp.Value;
                        _longestLineIndex = kvp.Key;
                    }
            }
            else if (newLength > _longestLineLength)
            {
                _longestLineLength = newLength;
                _longestLineIndex = lineIndex;
            }

            _scrollableViewModel.LongestLineWidth = _longestLineLength * CharWidth + LinePadding;
        }
        else
        {
            UpdateLineCache();
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
        _suppressScrollOnNextCursorMove = false;
        var shiftFlag = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        var ctrlFlag = (e.KeyModifiers & KeyModifiers.Control) != 0;

        // Don't clear selection or alter offsets for modifier keys
        if (e.Key == Key.LeftShift || e.Key == Key.RightShift ||
            e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
            e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
            e.Key == Key.CapsLock)
            return;

        if (ctrlFlag)
        {
            HandleControlKeyDown(e, viewModel);
            return;
        }

        // Initialize selection anchor only for arrow keys if Shift is pressed and no selection exists
        if (shiftFlag && _selectionAnchor == -1 && e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            if (viewModel.SelectionStart == 0 && viewModel.SelectionEnd == viewModel.Rope.Length)
                // Handle case when all text is selected
                _selectionAnchor = e.Key switch
                {
                    Key.Left => viewModel.SelectionEnd,
                    Key.Right => viewModel.SelectionStart,
                    Key.Up => viewModel.SelectionEnd,
                    Key.Down => viewModel.SelectionStart,
                    _ => viewModel.CursorPosition
                };
            else
                _selectionAnchor = viewModel.CursorPosition;
        }

        switch (e.Key)
        {
            case Key.Return:
                HandleReturn(viewModel);
                break;
            case Key.Back:
                HandleBackspace(viewModel);
                break;
            case Key.Delete:
                HandleDelete(viewModel);
                break;
            case Key.Left:
                if (shiftFlag)
                    // Handle Shift + Left Arrow
                    HandleShiftLeftArrow(viewModel);
                else
                    HandleLeftArrow(viewModel, shiftFlag);
                break;
            case Key.Right:
                if (shiftFlag)
                    // Handle Shift + Right Arrow
                    HandleShiftRightArrow(viewModel);
                else
                    HandleRightArrow(viewModel, shiftFlag);
                break;
            case Key.Up:
                if (shiftFlag)
                    // Handle Shift + Up Arrow
                    HandleShiftUpArrow(viewModel);
                else
                    HandleUpArrow(viewModel, shiftFlag);
                break;
            case Key.Down:
                if (shiftFlag)
                    // Handle Shift + Down Arrow
                    HandleShiftDownArrow(viewModel);
                else
                    HandleDownArrow(viewModel, shiftFlag);
                break;
            case Key.Home:
                HandleHome(viewModel, shiftFlag);
                break;
            case Key.End:
                HandleEnd(viewModel, shiftFlag);
                break;
        }

        // Only update the selection if shift is pressed and the key is not a character key
        if (shiftFlag && e.Key.ToString().Length != 1)
        {
            UpdateSelection(viewModel);
        }
        else
        {
            // Clear selection and reset anchor when Shift is not pressed
            viewModel.ClearSelection();
            _selectionAnchor = -1;
        }

        viewModel.CursorPosition = Math.Clamp(viewModel.CursorPosition, 0, viewModel.Rope.Length);
        InvalidateVisual();
    }

    private void HandleShiftLeftArrow(TextEditorViewModel viewModel)
    {
        if (viewModel.CursorPosition > 0)
        {
            viewModel.CursorPosition--;
            UpdateDesiredColumn(viewModel);
            viewModel.SelectionEnd = viewModel.CursorPosition;


            // Ensure SelectionStart is always less than or equal to SelectionEnd
            if (viewModel.SelectionStart > viewModel.SelectionEnd)
                (viewModel.SelectionStart, viewModel.SelectionEnd) = (viewModel.SelectionEnd, viewModel.SelectionStart);

            // Update the last known selection
            _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
        }
    }

    private void HandleShiftRightArrow(TextEditorViewModel viewModel)
    {
        // Do nothing if the cursor is at the end of the document
        if (viewModel.CursorPosition < viewModel.Rope.Length)
        {
            viewModel.CursorPosition++;
            UpdateDesiredColumn(viewModel);
            viewModel.SelectionEnd = viewModel.CursorPosition;
        }
    }

    private void HandleShiftUpArrow(TextEditorViewModel viewModel)
    {
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        if (currentLineIndex > 0)
        {
            var currentLineStart = viewModel.Rope.GetLineStartPosition(currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            _desiredColumn = Math.Max(_desiredColumn, currentColumn);

            var previousLineIndex = currentLineIndex - 1;
            var previousLineStart = viewModel.Rope.GetLineStartPosition(previousLineIndex);
            var previousLineLength = viewModel.Rope.GetLineLength(previousLineIndex);

            viewModel.CursorPosition = previousLineStart + Math.Min(_desiredColumn, previousLineLength - 1);
            viewModel.SelectionEnd = viewModel.CursorPosition;
        }
    }

    private void HandleShiftDownArrow(TextEditorViewModel viewModel)
    {
        // Do nothing if the cursor is at the end of the document
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        if (currentLineIndex < viewModel.Rope.GetLineCount() - 1)
        {
            var currentLineStart = viewModel.Rope.GetLineStartPosition(currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            _desiredColumn = Math.Max(_desiredColumn, currentColumn);

            var nextLineIndex = currentLineIndex + 1;
            var nextLineStart = viewModel.Rope.GetLineStartPosition(nextLineIndex);
            var nextLineLength = GetVisualLineLength(viewModel, nextLineIndex);

            viewModel.CursorPosition = nextLineStart + Math.Min(_desiredColumn, nextLineLength);
            viewModel.SelectionEnd = viewModel.CursorPosition;
        }
    }

    private void HandleDelete(TextEditorViewModel viewModel)
    {
        _suppressScrollOnNextCursorMove = true;
    
        if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1 &&
            viewModel.SelectionStart != viewModel.SelectionEnd)
        {
            // Handle deletion of selected text
            var start = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            var length = end - start;

            var lineIndex = GetLineIndex(viewModel, start);
            viewModel.DeleteText(start, length);
            OnTextDeleted(lineIndex, length);

            viewModel.CursorPosition = start;
            viewModel.ClearSelection();
        }
        else if (viewModel.CursorPosition < viewModel.Rope.Length)
        {
            // Handle deletion of a single character
            var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
            viewModel.DeleteText(viewModel.CursorPosition, 1);
            OnTextDeleted(lineIndex, 1);
        }
    }

    private void HandleEnd(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var lineEndPosition = viewModel.Rope.GetLineStartPosition(lineIndex) +
                              GetVisualLineLength(viewModel, lineIndex);
        viewModel.CursorPosition = lineEndPosition;
        UpdateDesiredColumn(viewModel);
        if (!isShiftPressed) viewModel.ClearSelection();
    }

    private void HandleHome(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        var lineStartPosition =
            viewModel.Rope.GetLineStartPosition(GetLineIndex(viewModel, viewModel.CursorPosition));
        viewModel.CursorPosition = lineStartPosition;
        _desiredColumn = 0;
        if (!isShiftPressed) viewModel.ClearSelection();
    }

    private void HandleLeftArrow(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the start of the selection
            viewModel.CursorPosition = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            viewModel.ClearSelection();
            return;
        }

        if (viewModel.CursorPosition > 0)
        {
            viewModel.CursorPosition--;
            UpdateDesiredColumn(viewModel);
            if (isShiftPressed)
                // Update selection
                viewModel.SelectionEnd = viewModel.CursorPosition;
            else
                viewModel.ClearSelection();
        }
    }

    private void HandleRightArrow(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the end of the selection
            viewModel.CursorPosition = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            viewModel.ClearSelection();
            return;
        }

        if (viewModel.CursorPosition < viewModel.Rope.Length)
        {
            viewModel.CursorPosition++;
            UpdateDesiredColumn(viewModel);
            if (isShiftPressed)
                // Update selection
                viewModel.SelectionEnd = viewModel.CursorPosition;
            else
                viewModel.ClearSelection();
        }
    }

    private void HandleUpArrow(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the start of the selection
            viewModel.CursorPosition = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            viewModel.ClearSelection();
            return;
        }

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

        if (isShiftPressed)
            // Update selection
            viewModel.SelectionEnd = viewModel.CursorPosition;
        else
            viewModel.ClearSelection();
    }

    private void HandleDownArrow(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the end of the selection
            viewModel.CursorPosition = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            viewModel.ClearSelection();
            return;
        }

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

        if (isShiftPressed)
            // Update selection
            viewModel.SelectionEnd = viewModel.CursorPosition;
        else
            viewModel.ClearSelection();
    }

    private void HandleBackspace(TextEditorViewModel viewModel)
    {
        if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1 &&
            viewModel.SelectionStart != viewModel.SelectionEnd)
        {
            // Handle deletion of selected text
            var start = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            var length = end - start;

            var lineIndex = GetLineIndex(viewModel, start);
            viewModel.DeleteText(start, length);
            OnTextDeleted(lineIndex, length);

            viewModel.CursorPosition = start;
            viewModel.ClearSelection();
        }
        else if (viewModel.CursorPosition > 0)
        {
            // Handle deletion of a single character before the cursor
            var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition - 1);
            viewModel.DeleteText(viewModel.CursorPosition - 1, 1);
            OnTextDeleted(lineIndex, 1);
            viewModel.CursorPosition--;
        }
    }

    private void HandleReturn(TextEditorViewModel viewModel)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd)
        {
            // Handle deletion of selected text
            var start = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            var length = end - start;

            viewModel.DeleteText(start, length);
            viewModel.CursorPosition = start;
        }

        var insertPosition = viewModel.CursorPosition;
        viewModel.InsertText(insertPosition, "\n");
        viewModel.CursorPosition = insertPosition + 1;

        // Clear selection after insertion
        viewModel.ClearSelection();
        _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition);
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
            _desiredColumn = 0;
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

        while (lineIndex < viewModel.Rope.LineCount &&
               accumulatedLength + viewModel.Rope.GetLineLength(lineIndex) <= position)
        {
            accumulatedLength += viewModel.Rope.GetLineLength(lineIndex);
            lineIndex++;
        }

        // Ensure lineIndex does not exceed the line count
        lineIndex = Math.Max(0, Math.Min(lineIndex, viewModel.Rope.LineCount - 1));

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

        if (viewModel.SelectionStart == viewModel.SelectionEnd) return;

        var selectionStart = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
        var selectionEnd = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
        var cursorPosition = viewModel.CursorPosition;

        var startLine = GetLineIndexFromPosition(selectionStart);
        var endLine = GetLineIndexFromPosition(selectionEnd);
        var cursorLine = GetLineIndexFromPosition(cursorPosition);

        var firstVisibleLine = Math.Max(0, (int)(scrollableViewModel.VerticalOffset / LineHeight));
        var lastVisibleLine = Math.Min(
            firstVisibleLine + (int)(viewableAreaHeight / LineHeight) + 1,
            GetLineCount());

        for (var i = Math.Max(startLine, firstVisibleLine); i <= Math.Min(endLine, lastVisibleLine); i++)
        {
            var lineStartOffset = i == startLine ? selectionStart - _lineStarts[i] : 0;
            var lineEndOffset = i == endLine ? selectionEnd - _lineStarts[i] : GetVisualLineLength(viewModel, i);

            if (i == cursorLine && cursorPosition == selectionEnd)
                lineEndOffset = Math.Min(lineEndOffset, cursorPosition - _lineStarts[i]);

            var xStart = lineStartOffset * CharWidth;
            var xEnd = lineEndOffset * CharWidth;
            var y = i * LineHeight;

            // Get the actual line length
            var actualLineLength = GetVisualLineLength(viewModel, i) * CharWidth;

            // Skip drawing selection if the line is empty and the cursor is on this line
            if (actualLineLength == 0 && i == cursorLine) continue;

            // Determine if this is the last line of selection
            var isLastSelectionLine = i == endLine;

            // Calculate the selection width, accounting for line length, padding, and empty lines
            var selectionWidth = xEnd - xStart;
            if (actualLineLength == 0) // Empty line
            {
                selectionWidth = CharWidth;
                if (!isLastSelectionLine) selectionWidth += SelectionEndPadding;
            }
            else if (xEnd > actualLineLength)
            {
                selectionWidth = Math.Min(selectionWidth, actualLineLength - xStart);
                if (!isLastSelectionLine) selectionWidth += SelectionEndPadding;
            }
            else if (!isLastSelectionLine)
            {
                selectionWidth += SelectionEndPadding;
            }

            // Ensure minimum width of one character
            selectionWidth = Math.Max(selectionWidth, CharWidth);

            var selectionRect = new Rect(xStart, y, selectionWidth, LineHeight);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(100, 139, 205, 192)), selectionRect);
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
            _selectionAnchor = 0;
            _lastKnownSelection = (0, viewModel.Rope.Length);
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

            viewModel.InsertText(viewModel.CursorPosition, text);
            OnTextInserted(GetLineIndex(viewModel, viewModel.CursorPosition), text.Length);
            viewModel.CursorPosition += text.Length;
            viewModel.CursorPosition = Math.Min(viewModel.CursorPosition, viewModel.Rope.Length);
            UpdateDesiredColumn(viewModel);

            UpdateLineCache(); // Ensure the cache is fully updated after pasting

            viewModel.ClearSelection(); // Clear selection after pasting
            _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition); // Update last known selection

            UpdateHorizontalScrollPosition();
            EnsureCursorVisible();
            InvalidateVisual();
        }
    }

    private void UpdateHorizontalScrollPosition()
    {
        if (_scrollableViewModel == null) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var cursorColumn = viewModel.CursorPosition - _lineStarts[cursorLine];
        var cursorX = cursorColumn * CharWidth;

        if (cursorX < _scrollableViewModel.HorizontalOffset)
            _scrollableViewModel.HorizontalOffset = cursorX;
        else if (cursorX > _scrollableViewModel.HorizontalOffset + _scrollableViewModel.Viewport.Width)
            _scrollableViewModel.HorizontalOffset = cursorX - _scrollableViewModel.Viewport.Width + CharWidth;
    }
}