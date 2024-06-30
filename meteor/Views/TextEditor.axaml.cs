using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.ViewModels;
using ReactiveUI;

namespace meteor.Views;

public partial class TextEditor : UserControl
{
    private const double DefaultFontSize = 13;
    private const double BaseLineHeight = 20;
    private double _fontSize = DefaultFontSize;
    private bool _suppressScrollOnNextCursorMove;
    private bool _isSelecting;
    private BigInteger _selectionAnchor = -1;
    private const double SelectionEndPadding = 2;
    private const double LinePadding = 20;
    private BigInteger _desiredColumn;
    private double _lineHeight = BaseLineHeight;
    private readonly List<BigInteger> _lineStarts = new();
    private BigInteger _cachedLineCount;
    private readonly Dictionary<BigInteger, BigInteger> _lineLengths = new();
    private BigInteger _longestLineIndex = -1;
    private BigInteger _longestLineLength;
    private ScrollableTextEditorViewModel _scrollableViewModel;
    private (BigInteger start, BigInteger end) _lastKnownSelection = (-1, -1);
    private readonly double _lineSpacingFactor = BaseLineHeight / DefaultFontSize;

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<TextEditor, FontFamily>(nameof(FontFamily),
            new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono"));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TextEditor, double>(nameof(FontSize), 13);

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<TextEditor, double>(nameof(LineHeight), 20.0);

    public double CharWidth
    {
        get
        {
            var formattedText = new FormattedText(
                "x",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily),
                FontSize,
                Brushes.Black);
            return formattedText.Width;
        }
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public TextEditor()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Focusable = true;

        FontFamily = new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono");

        // Add handlers for FontFamily, FontSize, and LineHeight changes
        this.GetObservable(FontFamilyProperty).Subscribe(OnFontFamilyChanged);
        this.GetObservable(FontSizeProperty).Subscribe(OnFontSizeChanged);
        this.GetObservable(LineHeightProperty).Subscribe(OnLineHeightChanged);

        // Initial measurement
        MeasureCharWidth();
    }

    private void OnLineHeightChanged(double newLineHeight)
    {
        _lineHeight = newLineHeight;
        InvalidateVisual();
    }

    private void OnFontSizeChanged(double newFontSize)
    {
        _fontSize = newFontSize;
        UpdateMetrics();
        InvalidateVisual();
    }

    private void OnFontFamilyChanged(FontFamily newFontFamily)
    {
        MeasureCharWidth();
        InvalidateVisual();
    }

    private void MeasureCharWidth()
    {
        var referenceText = new FormattedText(
            "0",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily),
            FontSize,
            Brushes.Black);

        // If _scrollableViewModel is available, update the LongestLineWidth
        if (_scrollableViewModel != null)
            _scrollableViewModel.LongestLineWidth =
                ConvertBigIntegerToDouble(_longestLineLength) * CharWidth + LinePadding;
    }


    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (_scrollableViewModel?.TextEditorViewModel != null)
            _scrollableViewModel.TextEditorViewModel.PropertyChanged -= ViewModel_PropertyChanged;

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            _scrollableViewModel = scrollableViewModel;
            var viewModel = scrollableViewModel.TextEditorViewModel;
            viewModel.LineHeight = LineHeight;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Bind the LineHeight property to the ViewModel
            Bind(LineHeightProperty, viewModel.WhenAnyValue(vm => vm.LineHeight));

            UpdateLineCache(-1);
        }
    }
    
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (_scrollableViewModel?.TextEditorViewModel != null)
            _scrollableViewModel.TextEditorViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void UpdateMetrics()
    {
        MeasureCharWidth();
        LineHeight = Math.Ceiling(_fontSize * _lineSpacingFactor);
    }

    private void UpdateLineCache(BigInteger changedLineIndex, int linesInserted = 0)
    {
        if (_scrollableViewModel == null) return;
        var viewModel = _scrollableViewModel.TextEditorViewModel;

        if (changedLineIndex == -1)
        {
            // Full update if no specific line index is provided (initial or major changes)
            _lineStarts.Clear();
            _lineLengths.Clear();
            _lineStarts.Add(BigInteger.Zero);

            var lineStart = BigInteger.Zero;
            while (lineStart < viewModel.Rope.Length)
            {
                var nextNewline = viewModel.Rope.IndexOf('\n', (int)lineStart);
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
            _longestLineLength = BigInteger.Zero;

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
        }
        else
        {
            // Ensure the changedLineIndex is within valid bounds
            var lineCount = viewModel.Rope.GetLineCount();
            if (lineCount == 0) return; // No lines to update

            if (changedLineIndex < 0)
                changedLineIndex = 0;
            else if (changedLineIndex >= lineCount) changedLineIndex = lineCount - 1;

            var lineStart = viewModel.Rope.GetLineStartPosition((int)changedLineIndex);
            var lineEnd = viewModel.Rope.GetLineEndPosition((int)changedLineIndex) + linesInserted;

            // Remove invalidated entries
            while (_lineStarts.Count > changedLineIndex + 1 &&
                   _lineStarts[(int)changedLineIndex + 1] <= lineEnd)
            {
                _lineStarts.RemoveAt((int)changedLineIndex + 1);
                _lineLengths.Remove(changedLineIndex);
            }

            // If the change resulted in removing the last line
            if (changedLineIndex >= lineCount)
            {
                _lineLengths[changedLineIndex - 1] = viewModel.Rope.Length - lineStart;
                _cachedLineCount = _lineStarts.Count;
                goto RecalculateLongestLine; // Skip recalculation of remaining lines
            }

            // Recalculate line starts and lengths from the changed line onwards
            while (lineStart < viewModel.Rope.Length)
            {
                var nextNewline = viewModel.Rope.IndexOf('\n', lineStart);
                if (nextNewline == -1)
                {
                    _lineLengths[changedLineIndex] = viewModel.Rope.Length - lineStart;
                    break;
                }

                if (_lineStarts.Count > changedLineIndex + 1)
                    _lineStarts[(int)changedLineIndex + 1] = nextNewline + 1;
                else
                    _lineStarts.Add(nextNewline + 1);

                _lineLengths[changedLineIndex] = nextNewline - lineStart;
                lineStart = nextNewline + 1;
                changedLineIndex++;
            }

            // Update the line count cache
            _cachedLineCount = _lineStarts.Count;
        }

        RecalculateLongestLine:
        // Recalculate longest line considering lines around the changed line
        BigInteger startLine = Math.Max(0, (int)changedLineIndex - 1);
        var endLine = BigInteger.Min(_cachedLineCount - 1, changedLineIndex + 1);

        for (var i = startLine; i <= endLine; i++)
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
        _scrollableViewModel.LongestLineWidth = (double)_longestLineLength * CharWidth + LinePadding;
    }

    private BigInteger GetLineLength(TextEditorViewModel viewModel, BigInteger lineIndex)
    {
        return _lineLengths.GetValueOrDefault(lineIndex, BigInteger.Zero);
    }

    private BigInteger GetLineCount()
    {
        return _cachedLineCount;
    }

    public string GetLineText(BigInteger lineIndex)
    {
        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            if (lineIndex < BigInteger.Zero || lineIndex >= viewModel.Rope.GetLineCount())
                return string.Empty; // Return empty string if line index is out of range

            return viewModel.Rope.GetLineText((int)lineIndex);
        }

        return string.Empty;
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TextEditorViewModel.Rope))
        {
            // Full update since the entire rope changed
            UpdateLineCache(-1); 
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
            viewModel.SelectionStart = BigInteger.Min(_selectionAnchor, viewModel.CursorPosition);
            viewModel.SelectionEnd = BigInteger.Max(_selectionAnchor, viewModel.CursorPosition);
            _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
        }
    }

    private void EnsureCursorVisible()
    {
        if (_scrollableViewModel == null) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var cursorColumn = viewModel.CursorPosition - _lineStarts[(int)cursorLine];

        if (!_suppressScrollOnNextCursorMove)
        {
            // Vertical scrolling
            var cursorY = (double)cursorLine * LineHeight;
            var bottomPadding = 5;
            // var verticalBufferLines = 3;
            var verticalBufferLines = 0;
            var verticalBufferHeight = verticalBufferLines * LineHeight;

            if (cursorY < _scrollableViewModel.VerticalOffset + verticalBufferHeight)
                _scrollableViewModel.VerticalOffset = Math.Max(0, cursorY - verticalBufferHeight);
            else if (cursorY + LineHeight + bottomPadding > _scrollableViewModel.VerticalOffset +
                     _scrollableViewModel.Viewport.Height - verticalBufferHeight)
                _scrollableViewModel.VerticalOffset = cursorY + LineHeight + bottomPadding -
                    _scrollableViewModel.Viewport.Height + verticalBufferHeight;

            // Horizontal scrolling
            var cursorX = (double)cursorColumn * CharWidth;
            var viewportWidth = _scrollableViewModel.Viewport.Width;
            var currentOffset = _scrollableViewModel.HorizontalOffset;

            // Margin to keep the cursor visible
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

    private BigInteger GetPositionFromPoint(Point point)
    {
        if (_scrollableViewModel == null)
            return BigInteger.Zero;

        var lineIndex = (BigInteger)(point.Y / LineHeight);
        var column = (BigInteger)(point.X / CharWidth);

        lineIndex = BigInteger.Max(BigInteger.Zero, BigInteger.Min(lineIndex, GetLineCount() - 1));
        var lineStart = _lineStarts[(int)lineIndex];
        var lineLength = GetVisualLineLength(_scrollableViewModel.TextEditorViewModel, lineIndex);
        column = BigInteger.Max(BigInteger.Zero, BigInteger.Min(column, lineLength));

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
                    var start = BigInteger.Min(_lastKnownSelection.start, _lastKnownSelection.end);
                    var end = BigInteger.Max(_lastKnownSelection.start, _lastKnownSelection.end);
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

    private void OnTextInserted(BigInteger lineIndex, BigInteger length)
    {
        if (_lineLengths.ContainsKey(lineIndex))
        {
            var newLength = GetVisualLineLength(_scrollableViewModel.TextEditorViewModel, lineIndex);
            _lineLengths[lineIndex] = newLength;
            if (newLength > _longestLineLength)
            {
                _longestLineLength = newLength;
                _longestLineIndex = lineIndex;
                _scrollableViewModel.LongestLineWidth = (double)_longestLineLength * CharWidth + LinePadding;
            }
        }
        else
        {
            UpdateLineCache(lineIndex, 1);
        }
    }

    private void OnTextDeleted(BigInteger lineIndex, BigInteger length)
    {
        if (_lineLengths.ContainsKey(lineIndex))
        {
            var newLength = GetVisualLineLength(_scrollableViewModel.TextEditorViewModel, lineIndex);
            _lineLengths[lineIndex] = newLength;

            // Recalculate the longest line if necessary
            if (newLength < _longestLineLength && lineIndex == _longestLineIndex)
            {
                _longestLineLength = BigInteger.Zero;
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

            _scrollableViewModel.LongestLineWidth = (double)_longestLineLength * CharWidth + LinePadding;
        }
        else
        {
            UpdateLineCache(lineIndex);
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
            case Key.PageUp:
                HandlePageUp(viewModel, shiftFlag);
                break;
            case Key.PageDown:
                HandlePageDown(viewModel, shiftFlag);
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

        viewModel.CursorPosition = BigInteger.Min(BigInteger.Max(viewModel.CursorPosition, BigInteger.Zero),
            viewModel.Rope.Length);
        InvalidateVisual();
    }

    private void HandlePageUp(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var linesPerPage = (BigInteger)(_scrollableViewModel.Viewport.Height / LineHeight);
        var newLineIndex = BigInteger.Max(BigInteger.Zero, currentLineIndex - linesPerPage);

        // Set cursor to the start of the first line if newLineIndex is 0
        var newCursorPosition = newLineIndex == BigInteger.Zero
            ? BigInteger.Zero
            : BigInteger.Min(viewModel.Rope.GetLineStartPosition((int)newLineIndex) + _desiredColumn,
                viewModel.Rope.GetLineStartPosition((int)newLineIndex) + GetVisualLineLength(viewModel, newLineIndex));

        viewModel.CursorPosition = newCursorPosition;

        if (!isShiftPressed)
            viewModel.ClearSelection();
        else
            viewModel.SelectionEnd = viewModel.CursorPosition;

        // Convert the viewport height and vertical offset to BigInteger before subtraction
        var viewportHeightBigInteger = (BigInteger)Math.Floor(_scrollableViewModel.Viewport.Height);
        var verticalOffsetBigInteger = (BigInteger)Math.Floor(_scrollableViewModel.VerticalOffset);

        _scrollableViewModel.VerticalOffset =
            (double)BigInteger.Max(BigInteger.Zero, verticalOffsetBigInteger - viewportHeightBigInteger);
    }

    private void HandlePageDown(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var linesPerPage = (BigInteger)(_scrollableViewModel.Viewport.Height / LineHeight);
        var newLineIndex = BigInteger.Min(GetLineCount() - 1, currentLineIndex + linesPerPage);

        // Set cursor to the end of the last line if newLineIndex is the last line
        var lastLineIndex = GetLineCount() - 1;
        var lineStart = viewModel.Rope.GetLineStartPosition((int)newLineIndex);
        var newCursorPosition = newLineIndex == lastLineIndex
            ? lineStart + GetVisualLineLength(viewModel, newLineIndex)
            : BigInteger.Min(lineStart + _desiredColumn, lineStart + GetVisualLineLength(viewModel, newLineIndex));

        viewModel.CursorPosition = newCursorPosition;

        if (!isShiftPressed)
            viewModel.ClearSelection();
        else
            viewModel.SelectionEnd = viewModel.CursorPosition;

        // Convert the viewport height and vertical offset to BigInteger before addition
        var viewportHeightBigInteger = (BigInteger)Math.Floor(_scrollableViewModel.Viewport.Height);
        var verticalOffsetBigInteger = (BigInteger)Math.Floor(_scrollableViewModel.VerticalOffset);

        _scrollableViewModel.VerticalOffset = (double)BigInteger.Min(
            verticalOffsetBigInteger + viewportHeightBigInteger,
            (_cachedLineCount - 1) * (BigInteger)LineHeight);
    }


    private void HandleShiftLeftArrow(TextEditorViewModel viewModel)
    {
        if (viewModel.CursorPosition > BigInteger.Zero)
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
        if (currentLineIndex > BigInteger.Zero)
        {
            var currentLineStart = viewModel.Rope.GetLineStartPosition((int)currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            _desiredColumn = BigInteger.Max(_desiredColumn, currentColumn);

            var previousLineIndex = currentLineIndex - 1;
            var previousLineStart = viewModel.Rope.GetLineStartPosition((int)previousLineIndex);
            var previousLineLength = viewModel.Rope.GetLineLength((int)previousLineIndex);

            viewModel.CursorPosition = previousLineStart + BigInteger.Min(_desiredColumn, previousLineLength - 1);
            viewModel.SelectionEnd = viewModel.CursorPosition;
        }
    }

    private void HandleShiftDownArrow(TextEditorViewModel viewModel)
    {
        // Do nothing if the cursor is at the end of the document
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        if (currentLineIndex < viewModel.Rope.GetLineCount() - 1)
        {
            var currentLineStart = viewModel.Rope.GetLineStartPosition((int)currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            _desiredColumn = BigInteger.Max(_desiredColumn, currentColumn);

            var nextLineIndex = currentLineIndex + 1;
            var nextLineStart = viewModel.Rope.GetLineStartPosition((int)nextLineIndex);
            var nextLineLength = GetVisualLineLength(viewModel, nextLineIndex);

            viewModel.CursorPosition = nextLineStart + BigInteger.Min(_desiredColumn, nextLineLength);
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
            var start = BigInteger.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = BigInteger.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
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
        var lineEndPosition = viewModel.Rope.GetLineStartPosition((int)lineIndex) +
                              GetVisualLineLength(viewModel, lineIndex);
        viewModel.CursorPosition = lineEndPosition;
        UpdateDesiredColumn(viewModel);
        if (!isShiftPressed) viewModel.ClearSelection();
    }

    private void HandleHome(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        var lineStartPosition =
            viewModel.Rope.GetLineStartPosition((int)GetLineIndex(viewModel, viewModel.CursorPosition));
        viewModel.CursorPosition = lineStartPosition;
        _desiredColumn = BigInteger.Zero;
        if (!isShiftPressed) viewModel.ClearSelection();
    }

    private void HandleLeftArrow(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the start of the selection
            viewModel.CursorPosition = BigInteger.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            _lastKnownSelection = new ValueTuple<BigInteger, BigInteger>(-1, -1);
            viewModel.ClearSelection();
            return;
        }

        if (viewModel.CursorPosition > BigInteger.Zero)
        {
            viewModel.CursorPosition--;
            UpdateDesiredColumn(viewModel);
            if (isShiftPressed)
                // Update selection
                viewModel.SelectionEnd = viewModel.CursorPosition;
            else
            {
                _lastKnownSelection = new ValueTuple<BigInteger, BigInteger>(-1, -1);
                viewModel.ClearSelection();
            }
        }
    }

    private void HandleRightArrow(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the end of the selection
            viewModel.CursorPosition = BigInteger.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            _lastKnownSelection = new ValueTuple<BigInteger, BigInteger>(-1, -1);
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
            {
                _lastKnownSelection = new ValueTuple<BigInteger, BigInteger>(-1, -1);
                viewModel.ClearSelection();
            }
        }
    }

    private void HandleUpArrow(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the start of the selection
            viewModel.CursorPosition = BigInteger.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            viewModel.ClearSelection();
            return;
        }

        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        if (currentLineIndex > BigInteger.Zero)
        {
            var currentLineStart = viewModel.Rope.GetLineStartPosition((int)currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            // Update desired column only if it's greater than the current column
            _desiredColumn = BigInteger.Max(_desiredColumn, currentColumn);

            var previousLineIndex = currentLineIndex - 1;
            var previousLineStart = viewModel.Rope.GetLineStartPosition((int)previousLineIndex);
            var previousLineLength = viewModel.Rope.GetLineLength((int)previousLineIndex);

            // Calculate new cursor position
            viewModel.CursorPosition = previousLineStart + BigInteger.Min(_desiredColumn, previousLineLength - 1);
        }
        else
        {
            // Move to the start of the first line
            viewModel.CursorPosition = BigInteger.Zero;
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
            viewModel.CursorPosition = BigInteger.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            viewModel.ClearSelection();
            return;
        }

        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        if (currentLineIndex < viewModel.Rope.GetLineCount() - 1)
        {
            var currentLineStart = viewModel.Rope.GetLineStartPosition((int)currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            // Update the desired column only if it's greater than the current column
            _desiredColumn = BigInteger.Max(_desiredColumn, currentColumn);

            var nextLineIndex = currentLineIndex + 1;
            var nextLineStart = viewModel.Rope.GetLineStartPosition((int)nextLineIndex);
            var nextLineLength = GetVisualLineLength(viewModel, nextLineIndex);

            // Calculate new cursor position
            viewModel.CursorPosition = nextLineStart + BigInteger.Min(_desiredColumn, nextLineLength);
        }
        else
        {
            // If the document is empty or at the end of the last line, set cursor to the end of the document
            if (viewModel.Rope.Length == BigInteger.Zero)
            {
                viewModel.CursorPosition = BigInteger.Zero;
            }
            else
            {
                var lastLineStart = viewModel.Rope.GetLineStartPosition((int)currentLineIndex);
                var lastLineLength = viewModel.Rope.GetLineLength((int)currentLineIndex);
                viewModel.CursorPosition = lastLineStart + lastLineLength;
            }
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
            var start = BigInteger.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = BigInteger.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            var length = end - start;

            var lineIndex = GetLineIndex(viewModel, start);
            viewModel.DeleteText(start, length);
            OnTextDeleted(lineIndex, length);

            viewModel.CursorPosition = start;
            viewModel.ClearSelection();
        }
        else if (viewModel.CursorPosition > BigInteger.Zero)
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
            var start = BigInteger.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = BigInteger.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
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
        var shiftFlag = (keyEventArgs.KeyModifiers & KeyModifiers.Shift) != 0;

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
            case Key.Left:
                MoveCursorToPreviousWord(viewModel, shiftFlag);
                break;
            case Key.Right:
                MoveCursorToNextWord(viewModel, shiftFlag);
                break;
            case Key.Up:
                ScrollViewport(-LineHeight);
                break;
            case Key.Down:
                ScrollViewport(LineHeight);
                break;
        }

        InvalidateVisual();
    }

    private void MoveCursorToPreviousWord(TextEditorViewModel viewModel, bool extendSelection)
    {
        var cursorPosition = viewModel.CursorPosition;

        if (cursorPosition == BigInteger.Zero)
            return;

        var lineIndex = GetLineIndex(viewModel, cursorPosition);
        var lineStart = viewModel.Rope.GetLineStartPosition((int)lineIndex);

        if (cursorPosition == lineStart)
        {
            // If at the start of a line, move to the end of the previous line
            if (lineIndex > 0)
            {
                var previousLineIndex = lineIndex - 1;
                viewModel.CursorPosition = viewModel.Rope.GetLineEndPosition((int)previousLineIndex);
            }

            return;
        }

        var lineText = viewModel.Rope.GetText(lineStart, (int)(cursorPosition - lineStart));
        var index = lineText.Length - 1;

        // Move left past any whitespace
        while (index > 0 && char.IsWhiteSpace(lineText[index - 1]))
        {
            index--;
            viewModel.CursorPosition--;
        }

        if (index > 0)
        {
            if (IsCommonCodingSymbol(lineText[index - 1]))
                // Move left past consecutive coding symbols
                while (index > 0 && IsCommonCodingSymbol(lineText[index - 1]))
                    index--;
            else
                // Move left until the next whitespace or coding symbol
                while (index > 0 && !char.IsWhiteSpace(lineText[index - 1]) &&
                       !IsCommonCodingSymbol(lineText[index - 1]))
                    index--;
        }

        viewModel.CursorPosition = lineStart + index;
        if (extendSelection)
        {
            viewModel.SelectionEnd = viewModel.CursorPosition;
            UpdateSelection(viewModel);
            _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
        }
        else
        {
            viewModel.ClearSelection();
            _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition);
        }

        InvalidateVisual();
    }

    private void MoveCursorToNextWord(TextEditorViewModel viewModel, bool extendSelection)
    {
        var cursorPosition = viewModel.CursorPosition;
        var lineIndex = GetLineIndex(viewModel, cursorPosition);
        var lineStart = viewModel.Rope.GetLineStartPosition((int)lineIndex);
        var lineEnd = viewModel.Rope.GetLineEndPosition((int)lineIndex);

        if (cursorPosition >= lineEnd)
        {
            // Move to the start of the next line if at the end of the current line
            if (lineIndex < viewModel.Rope.GetLineCount() - 1)
                viewModel.CursorPosition = viewModel.Rope.GetLineStartPosition((int)lineIndex + 1);
            return;
        }

        var lineText = viewModel.Rope.GetText((int)cursorPosition, (int)(lineEnd - cursorPosition));
        var index = 0;

        // Move through whitespace, updating cursor position for each character
        while (index < lineText.Length && char.IsWhiteSpace(lineText[index]))
        {
            viewModel.CursorPosition = cursorPosition + index + 1;
            index++;
            viewModel.CursorPosition++;
        }

        // If we're still within the line after moving through whitespace
        if (index < lineText.Length)
        {
            if (IsCommonCodingSymbol(lineText[index]))
                // Move through coding symbols
                while (index < lineText.Length && IsCommonCodingSymbol(lineText[index]))
                {
                    viewModel.CursorPosition = cursorPosition + index + 1;
                    index++;
                }
            else
                // Move through word characters
                while (index < lineText.Length && !char.IsWhiteSpace(lineText[index]) &&
                       !IsCommonCodingSymbol(lineText[index]))
                {
                    viewModel.CursorPosition = cursorPosition + index + 1;
                    index++;
                }
        }

        if (extendSelection)
        {
            viewModel.SelectionEnd = viewModel.CursorPosition;
            UpdateSelection(viewModel);
            _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
        }
        else
        {
            viewModel.ClearSelection();
            _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition);
        }

        InvalidateVisual();
    }

    private bool IsCommonCodingSymbol(char c)
    {
        return "(){}[]<>.,;:'\"\\|`~!@#$%^&*-+=/?".Contains(c);
    }

    private void ScrollViewport(double delta)
    {
        if (_scrollableViewModel != null)
        {
            var newOffset = _scrollableViewModel.VerticalOffset + delta;
            var maxOffset = (double)GetLineCount() * LineHeight - _scrollableViewModel.Viewport.Height;
            _scrollableViewModel.VerticalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));
        }
    }

    private void UpdateDesiredColumn(TextEditorViewModel viewModel)
    {
        var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);

        // Update line cache only if the needed line is not yet calculated
        if (lineIndex >= _lineStarts.Count) UpdateLineCache(lineIndex);

        // Bounds check and calculate _desiredColumn
        if (lineIndex >= BigInteger.Zero && lineIndex < _lineStarts.Count)
        {
            var lineStart = _lineStarts[(int)lineIndex];
            _desiredColumn = viewModel.CursorPosition - lineStart;
        }
        else
        {
            _desiredColumn = BigInteger.Zero;
        }
    }

    private BigInteger GetVisualLineLength(TextEditorViewModel viewModel, BigInteger lineIndex)
    {
        var lineLength = GetLineLength(viewModel, lineIndex);

        // Subtract 1 if the line ends with a newline character
        if (lineLength > BigInteger.Zero &&
            viewModel.Rope.GetText(viewModel.Rope.GetLineStartPosition((int)lineIndex) + (int)lineLength - 1, 1) ==
            "\n")
            lineLength--;

        return lineLength;
    }

    private BigInteger GetLineIndex(TextEditorViewModel viewModel, BigInteger position)
    {
        if (viewModel.Rope == null) throw new InvalidOperationException("Rope is not initialized in the ViewModel.");

        var lineIndex = BigInteger.Zero;
        var accumulatedLength = BigInteger.Zero;

        while (lineIndex < viewModel.Rope.LineCount &&
               accumulatedLength + viewModel.Rope.GetLineLength((int)lineIndex) <= position)
        {
            accumulatedLength += viewModel.Rope.GetLineLength((int)lineIndex);
            lineIndex++;
        }

        // Ensure lineIndex does not exceed the line count
        lineIndex = BigInteger.Max(BigInteger.Zero, BigInteger.Min(lineIndex, viewModel.Rope.LineCount - 1));

        return lineIndex;
    }

    public override void Render(DrawingContext context)
    {
        if (_scrollableViewModel == null) return;

        context.FillRectangle(Brushes.White, new Rect(Bounds.Size));

        var lineCount = GetLineCount();
        if (lineCount == BigInteger.Zero) return;

        var viewableAreaWidth = _scrollableViewModel.Viewport.Width + LinePadding;
        var viewableAreaHeight = _scrollableViewModel.Viewport.Height;

        var firstVisibleLine = Math.Max(0,
            _scrollableViewModel.VerticalOffset / LineHeight - 5);
        var lastVisibleLine = BigInteger.Min(
            (BigInteger)(firstVisibleLine + viewableAreaHeight / LineHeight + 10),
            lineCount);

        // Draw background for the current line
        var viewModel = _scrollableViewModel.TextEditorViewModel;
        RenderCurrentLine(context, viewModel, viewableAreaWidth);

        RenderVisibleLines(context, _scrollableViewModel, (BigInteger)firstVisibleLine, lastVisibleLine,
            viewableAreaWidth);
        DrawSelection(context, viewableAreaWidth, viewableAreaHeight, _scrollableViewModel);
        DrawCursor(context, viewableAreaWidth, viewableAreaHeight, _scrollableViewModel);
    }

    private void RenderCurrentLine(DrawingContext context, TextEditorViewModel viewModel, double viewableAreaWidth)
    {
        // Draw background for the current line
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var y = (double)cursorLine * LineHeight;

        // Check if the current line is part of the selection
        var selectionStartLine = GetLineIndexFromPosition(viewModel.SelectionStart);
        var selectionEndLine = GetLineIndexFromPosition(viewModel.SelectionEnd);

        if (cursorLine < selectionStartLine || cursorLine > selectionEndLine)
        {
            var rect = new Rect(0, y, viewableAreaWidth + _scrollableViewModel.HorizontalOffset, LineHeight);
            context.FillRectangle(new SolidColorBrush(Color.Parse("#ededed")), rect);
        }
        else
        {
            // Render the part of the current line that is not selected
            var selectionStart = BigInteger.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var selectionEnd = BigInteger.Max(viewModel.SelectionStart, viewModel.SelectionEnd);

            var lineStartOffset = cursorLine == selectionStartLine
                ? selectionStart - _lineStarts[(int)cursorLine]
                : BigInteger.Zero;
            var lineEndOffset = cursorLine == selectionEndLine
                ? selectionEnd - _lineStarts[(int)cursorLine]
                : GetVisualLineLength(viewModel, cursorLine);

            var xStart = (double)lineStartOffset * CharWidth;
            var xEnd = (double)lineEndOffset * CharWidth;

            // Draw the part of the line before the selection
            if (xStart > 0)
            {
                var beforeSelectionRect = new Rect(0, y, xStart, LineHeight);
                context.FillRectangle(new SolidColorBrush(Color.Parse("#ededed")), beforeSelectionRect);
            }

            // Draw the part of the line after the selection
            var afterSelectionRect = new Rect(xEnd, y,
                viewableAreaWidth + _scrollableViewModel.HorizontalOffset - xEnd, LineHeight);
            context.FillRectangle(new SolidColorBrush(Color.Parse("#ededed")), afterSelectionRect);
        }
    }

    private void RenderVisibleLines(DrawingContext context, ScrollableTextEditorViewModel scrollableViewModel,
        BigInteger firstVisibleLine, BigInteger lastVisibleLine, double viewableAreaWidth)
    {
        const int startIndexBuffer = 5;
        var yOffset = (double)firstVisibleLine * LineHeight;

        for (var i = firstVisibleLine; i < lastVisibleLine; i++)
        {
            var lineText = GetLineText((int)i); // Convert BigInteger to int for method call
            var xOffset = 0;

            if (string.IsNullOrEmpty(lineText))
            {
                // Handle empty lines
                yOffset += LineHeight;
                continue;
            }

            // Calculate the start index and the number of characters to display based on the visible area width
            var startIndex = BigInteger.Max(BigInteger.Zero,
                ConvertDoubleToBigInteger(scrollableViewModel.HorizontalOffset / CharWidth) - startIndexBuffer);

            // Ensure startIndex is within the lineText length
            if (startIndex >= lineText.Length) startIndex = BigInteger.Max(BigInteger.Zero, lineText.Length - 1);

            var maxCharsToDisplay = BigInteger.Min(lineText.Length - startIndex,
                ConvertDoubleToBigInteger((viewableAreaWidth - LinePadding) / CharWidth) + startIndexBuffer * 2);

            // Ensure maxCharsToDisplay is non-negative
            if (maxCharsToDisplay < BigInteger.Zero) maxCharsToDisplay = BigInteger.Zero;

            // Get the visible part of the line text
            var visiblePart = lineText.Substring((int)startIndex, (int)maxCharsToDisplay);

            var formattedText = new FormattedText(
                visiblePart,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily),
                FontSize,
                Brushes.Black);

            // Calculate the vertical offset to center the text
            var verticalOffset = (LineHeight - formattedText.Height) / 2;

            context.DrawText(formattedText,
                new Point(xOffset + (double)startIndex * CharWidth, yOffset + verticalOffset));

            yOffset += LineHeight;
        }
    }

    private BigInteger ConvertDoubleToBigInteger(double value)
    {
        return (BigInteger)Math.Floor(value);
    }

    private double ConvertBigIntegerToDouble(BigInteger value)
    {
        return (double)value;
    }

    private void DrawSelection(DrawingContext context, double viewableAreaWidth,
        double viewableAreaHeight, ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        if (viewModel.SelectionStart == viewModel.SelectionEnd) return;

        var selectionStart = BigInteger.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
        var selectionEnd = BigInteger.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
        var cursorPosition = viewModel.CursorPosition;

        var startLine = GetLineIndexFromPosition(selectionStart);
        var endLine = GetLineIndexFromPosition(selectionEnd);
        var cursorLine = GetLineIndexFromPosition(cursorPosition);

        var firstVisibleLine = BigInteger.Max(BigInteger.Zero,
            ConvertDoubleToBigInteger(_scrollableViewModel.VerticalOffset / LineHeight) - 5);
        var lastVisibleLine = BigInteger.Min(
            firstVisibleLine + ConvertDoubleToBigInteger(viewableAreaHeight / LineHeight) + 1 + 5,
            GetLineCount());

        for (var i = BigInteger.Max(startLine, firstVisibleLine); i <= BigInteger.Min(endLine, lastVisibleLine); i++)
        {
            var lineStartOffset = i == startLine ? selectionStart - _lineStarts[(int)i] : BigInteger.Zero;
            var lineEndOffset = i == endLine ? selectionEnd - _lineStarts[(int)i] : GetVisualLineLength(viewModel, i);

            if (i == cursorLine && cursorPosition == selectionEnd)
                lineEndOffset = BigInteger.Min(lineEndOffset, cursorPosition - _lineStarts[(int)i]);

            var xStart = (double)lineStartOffset * CharWidth;
            var xEnd = (double)lineEndOffset * CharWidth;
            var y = (double)i * LineHeight;

            // Get the actual line length
            var actualLineLength = (double)GetVisualLineLength(viewModel, i) * CharWidth;

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
        var lineStartPosition = _lineStarts[(int)cursorLine];
        var cursorColumn = viewModel.CursorPosition - lineStartPosition;

        // Calculate cursor position relative to the visible area
        var cursorXRelative = (double)cursorColumn * CharWidth;

        // Use the helper method to convert BigInteger to double for the calculation
        var cursorY = (double)cursorLine * LineHeight;

        if (cursorXRelative >= 0)
            context.DrawLine(
                new Pen(Brushes.Black),
                new Point(cursorXRelative, cursorY),
                new Point(cursorXRelative, cursorY + LineHeight)
            );
    }

    private BigInteger GetLineIndexFromPosition(BigInteger position)
    {
        var index = _lineStarts.BinarySearch((int)position);
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
            viewModel.SelectionStart = BigInteger.Zero;
            viewModel.SelectionEnd = viewModel.Rope.Length;
            viewModel.CursorPosition = viewModel.Rope.Length;
            _selectionAnchor = BigInteger.Zero;
            _lastKnownSelection = (BigInteger.Zero, viewModel.Rope.Length);
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

            var selectionStart = BigInteger.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var selectionEnd = BigInteger.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            var selectedText = viewModel.Rope.GetText()
                .Substring((int)selectionStart, (int)(selectionEnd - selectionStart));

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
            var insertPosition = viewModel.CursorPosition;
            var lineIndex = GetLineIndex(viewModel, insertPosition);
            var linesInserted = text.Split('\n').Length; // Count newlines for line cache optimization

            // Delete selected text (if any) with optimization
            if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
            {
                var start = BigInteger.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
                var length = BigInteger.Abs(viewModel.SelectionEnd - viewModel.SelectionStart);
                viewModel.DeleteText(start, length);
                OnTextDeleted(lineIndex, length);
                insertPosition = start;
            }

            viewModel.InsertText(insertPosition, text);
            OnTextInserted(lineIndex, text.Length);

            viewModel.CursorPosition += text.Length;
            viewModel.CursorPosition = BigInteger.Min(viewModel.CursorPosition, viewModel.Rope.Length);
            UpdateDesiredColumn(viewModel);

            viewModel.ClearSelection();
            _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition);

            // Delay or throttle horizontal scroll updates here
            UpdateHorizontalScrollPosition();
            EnsureCursorVisible();

            // Redraw only the affected lines instead of the entire TextEditor
            InvalidateVisual();
        }
    }

    private void UpdateHorizontalScrollPosition()
    {
        if (_scrollableViewModel == null) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var cursorColumn = viewModel.CursorPosition - _lineStarts[(int)cursorLine];
        var cursorX = (double)cursorColumn * CharWidth;

        if (cursorX < _scrollableViewModel.HorizontalOffset)
            _scrollableViewModel.HorizontalOffset = cursorX;
        else if (cursorX > _scrollableViewModel.HorizontalOffset + _scrollableViewModel.Viewport.Width)
            _scrollableViewModel.HorizontalOffset =
                cursorX - _scrollableViewModel.Viewport.Width + CharWidth;
    }


}