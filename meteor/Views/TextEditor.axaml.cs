using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
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
    private const double SelectionEndPadding = 2;
    private const double LinePadding = 20;
    private readonly double _lineSpacingFactor = BaseLineHeight / DefaultFontSize;
    private readonly Dictionary<long, long> _lineLengths = new();
    
    private double _fontSize = DefaultFontSize;
    private double _lineHeight = BaseLineHeight;
    private bool _suppressScrollOnNextCursorMove;
    private bool _isSelecting;
    private long _selectionAnchor = -1;
    private long _desiredColumn;
    private long _longestLineLength;
    private ScrollableTextEditorViewModel _scrollableViewModel;
    private (long start, long end) _lastKnownSelection = (-1, -1);

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<TextEditor, FontFamily>(nameof(FontFamily),
            new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono"));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TextEditor, double>(nameof(FontSize), 13);

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<TextEditor, double>(nameof(LineHeight), 20.0);

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
                ConvertlongToDouble(_longestLineLength) * _scrollableViewModel.TextEditorViewModel.CharWidth +
                LinePadding;
    }

    private void UpdateMetrics()
    {
        MeasureCharWidth();
        LineHeight = Math.Ceiling(_fontSize * _lineSpacingFactor);
    }

    private void UpdateLineCache(long changedLineIndex, int linesInserted = 0)
    {
        var textBuffer = _scrollableViewModel?.TextEditorViewModel?.TextBuffer;
        textBuffer?.UpdateLineCache();
    }

    private long GetLineLength(TextEditorViewModel viewModel, long lineIndex)
    {
        return _lineLengths.GetValueOrDefault(lineIndex, 0);
    }

    private long GetLineCount()
    {
        return _scrollableViewModel.TextEditorViewModel.TextBuffer.LineCount;
    }

    public string GetLineText(long lineIndex)
    {
        if (_scrollableViewModel == null)
            return string.Empty;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        if (lineIndex < 0 || lineIndex >= viewModel.TextBuffer.LineCount)
            return string.Empty;

        return viewModel.TextBuffer.GetLineText((int)lineIndex);
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TextEditorViewModel.TextBuffer))
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
            viewModel.SelectionStart = long.Min(_selectionAnchor, viewModel.CursorPosition);
            viewModel.SelectionEnd = long.Max(_selectionAnchor, viewModel.CursorPosition);
            _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
        }
    }

    private void EnsureCursorVisible()
    {
        if (_scrollableViewModel == null ||
            _scrollableViewModel.TextEditorViewModel.ShouldScrollToCursor == false) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var cursorColumn = viewModel.CursorPosition -
                           _scrollableViewModel.TextEditorViewModel.TextBuffer.LineStarts[(int)cursorLine];

        if (!_suppressScrollOnNextCursorMove)
        {
            // Vertical scrolling
            var cursorY = cursorLine * LineHeight;
            var bottomPadding = 5;
            var verticalBufferLines = 0;
            var verticalBufferHeight = verticalBufferLines * LineHeight;

            if (cursorY < _scrollableViewModel.VerticalOffset + verticalBufferHeight)
                _scrollableViewModel.VerticalOffset = Math.Max(0, cursorY - verticalBufferHeight);
            else if (cursorY + LineHeight + bottomPadding > _scrollableViewModel.VerticalOffset +
                     _scrollableViewModel.Viewport.Height - verticalBufferHeight)
                _scrollableViewModel.VerticalOffset = cursorY + LineHeight + bottomPadding -
                    _scrollableViewModel.Viewport.Height + verticalBufferHeight;

            // Horizontal scrolling
            if (_scrollableViewModel.DisableHorizontalScrollToCursor)
            {
                _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
                InvalidateVisual();
                return;
            }

            var cursorX = cursorColumn * _scrollableViewModel.TextEditorViewModel.CharWidth;
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

    private long GetPositionFromPoint(Point point)
    {
        if (_scrollableViewModel == null)
            return 0;

        var lineIndex = (long)(point.Y / LineHeight);
        var column = (long)(point.X / _scrollableViewModel.TextEditorViewModel.CharWidth);

        lineIndex = long.Max(0, long.Min(lineIndex, GetLineCount() - 1));
        var lineStart = _scrollableViewModel.TextEditorViewModel.TextBuffer.LineStarts[(int)lineIndex];
        var lineLength = GetVisualLineLength(_scrollableViewModel.TextEditorViewModel, lineIndex);
        column = long.Max(0, long.Min(column, lineLength));

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
                    var start = long.Min(_lastKnownSelection.start, _lastKnownSelection.end);
                    var end = long.Max(_lastKnownSelection.start, _lastKnownSelection.end);
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

    private void OnTextInserted(long lineIndex, long length)
    {
        UpdateLineCache(lineIndex, 1); 
    }

    private void OnTextDeleted(long lineIndex, long length)
    {
        UpdateLineCache(lineIndex); 
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
            if (viewModel.SelectionStart == 0 && viewModel.SelectionEnd == viewModel.TextBuffer.Length)
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

        viewModel.CursorPosition = long.Min(long.Max(viewModel.CursorPosition, 0),
            viewModel.TextBuffer.Length);
        InvalidateVisual();
    }

    private void HandlePageUp(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var linesPerPage = (long)(_scrollableViewModel.Viewport.Height / LineHeight);
        var newLineIndex = long.Max(0, currentLineIndex - linesPerPage);

        // Set cursor to the start of the first line if newLineIndex is 0
        var newCursorPosition = newLineIndex == 0
            ? 0
            : long.Min(viewModel.TextBuffer.GetLineStartPosition((int)newLineIndex) + _desiredColumn,
                viewModel.TextBuffer.GetLineStartPosition((int)newLineIndex) +
                GetVisualLineLength(viewModel, newLineIndex));

        viewModel.CursorPosition = newCursorPosition;

        if (!isShiftPressed)
            viewModel.ClearSelection();
        else
            viewModel.SelectionEnd = viewModel.CursorPosition;

        // Convert the viewport height and vertical offset to long before subtraction
        var viewportHeightlong = (long)Math.Floor(_scrollableViewModel.Viewport.Height);
        var verticalOffsetlong = (long)Math.Floor(_scrollableViewModel.VerticalOffset);

        _scrollableViewModel.VerticalOffset =
            long.Max(0, verticalOffsetlong - viewportHeightlong);
    }

    private void HandlePageDown(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var linesPerPage = (long)(_scrollableViewModel.Viewport.Height / LineHeight);
        var newLineIndex = long.Min(GetLineCount() - 1, currentLineIndex + linesPerPage);

        // Set cursor to the end of the last line if newLineIndex is the last line
        var lastLineIndex = GetLineCount() - 1;
        var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)newLineIndex);
        var newCursorPosition = newLineIndex == lastLineIndex
            ? lineStart + GetVisualLineLength(viewModel, newLineIndex)
            : long.Min(lineStart + _desiredColumn, lineStart + GetVisualLineLength(viewModel, newLineIndex));

        viewModel.CursorPosition = newCursorPosition;

        if (!isShiftPressed)
            viewModel.ClearSelection();
        else
            viewModel.SelectionEnd = viewModel.CursorPosition;

        // Convert the viewport height and vertical offset to long before addition
        var viewportHeightlong = (long)Math.Floor(_scrollableViewModel.Viewport.Height);
        var verticalOffsetlong = (long)Math.Floor(_scrollableViewModel.VerticalOffset);

        _scrollableViewModel.VerticalOffset = long.Min(
            verticalOffsetlong + viewportHeightlong,
            (GetLineCount() - 1) * (long)LineHeight);
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
        if (viewModel.CursorPosition < viewModel.TextBuffer.Length)
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
            var currentLineStart = viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            _desiredColumn = long.Max(_desiredColumn, currentColumn);

            var previousLineIndex = currentLineIndex - 1;
            var previousLineStart = viewModel.TextBuffer.GetLineStartPosition((int)previousLineIndex);
            var previousLineLength = viewModel.TextBuffer.GetLineLength((int)previousLineIndex);

            viewModel.CursorPosition = previousLineStart + long.Min(_desiredColumn, previousLineLength - 1);
            viewModel.SelectionEnd = viewModel.CursorPosition;
        }
    }

    private void HandleShiftDownArrow(TextEditorViewModel viewModel)
    {
        // Do nothing if the cursor is at the end of the document
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        if (currentLineIndex < viewModel.TextBuffer.LineCount - 1)
        {
            var currentLineStart = viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            _desiredColumn = long.Max(_desiredColumn, currentColumn);

            var nextLineIndex = currentLineIndex + 1;
            var nextLineStart = viewModel.TextBuffer.GetLineStartPosition((int)nextLineIndex);
            var nextLineLength = GetVisualLineLength(viewModel, nextLineIndex);

            viewModel.CursorPosition = nextLineStart + long.Min(_desiredColumn, nextLineLength);
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
            var start = long.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = long.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            var length = end - start;

            var lineIndex = GetLineIndex(viewModel, start);
            viewModel.DeleteText(start, length);
            OnTextDeleted(lineIndex, length);

            viewModel.CursorPosition = start;
            viewModel.ClearSelection();
        }
        else if (viewModel.CursorPosition < viewModel.TextBuffer.Length)
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
        var lineEndPosition = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex) +
                              GetVisualLineLength(viewModel, lineIndex);
        viewModel.CursorPosition = lineEndPosition;
        UpdateDesiredColumn(viewModel);
        if (!isShiftPressed) viewModel.ClearSelection();
    }

    private void HandleHome(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        var lineStartPosition =
            viewModel.TextBuffer.GetLineStartPosition((int)GetLineIndex(viewModel, viewModel.CursorPosition));
        viewModel.CursorPosition = lineStartPosition;
        _desiredColumn = 0;
        if (!isShiftPressed) viewModel.ClearSelection();
    }

    private void HandleLeftArrow(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the start of the selection
            viewModel.CursorPosition = long.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            _lastKnownSelection = new ValueTuple<long, long>(-1, -1);
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
            {
                _lastKnownSelection = new ValueTuple<long, long>(-1, -1);
                viewModel.ClearSelection();
            }
        }
    }

    private void HandleRightArrow(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the end of the selection
            viewModel.CursorPosition = long.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            _lastKnownSelection = new ValueTuple<long, long>(-1, -1);
            viewModel.ClearSelection();
            return;
        }

        if (viewModel.CursorPosition < viewModel.TextBuffer.Length)
        {
            viewModel.CursorPosition++;
            UpdateDesiredColumn(viewModel);
            if (isShiftPressed)
                // Update selection
                viewModel.SelectionEnd = viewModel.CursorPosition;
            else
            {
                _lastKnownSelection = new ValueTuple<long, long>(-1, -1);
                viewModel.ClearSelection();
            }
        }
    }

    private void HandleUpArrow(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd && !isShiftPressed)
        {
            // Move cursor to the start of the selection
            viewModel.CursorPosition = long.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            viewModel.ClearSelection();
            return;
        }

        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        if (currentLineIndex > 0)
        {
            var currentLineStart = viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            // Update desired column only if it's greater than the current column
            _desiredColumn = long.Max(_desiredColumn, currentColumn);

            var previousLineIndex = currentLineIndex - 1;
            var previousLineStart = viewModel.TextBuffer.GetLineStartPosition((int)previousLineIndex);
            var previousLineLength = viewModel.TextBuffer.GetLineLength((int)previousLineIndex);

            // Calculate new cursor position
            viewModel.CursorPosition = previousLineStart + long.Min(_desiredColumn, previousLineLength - 1);
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
            viewModel.CursorPosition = long.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            viewModel.ClearSelection();
            return;
        }

        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        if (currentLineIndex < viewModel.TextBuffer.LineCount - 1)
        {
            var currentLineStart = viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
            var currentColumn = viewModel.CursorPosition - currentLineStart;

            // Update the desired column only if it's greater than the current column
            _desiredColumn = long.Max(_desiredColumn, currentColumn);

            var nextLineIndex = currentLineIndex + 1;
            var nextLineStart = viewModel.TextBuffer.GetLineStartPosition((int)nextLineIndex);
            var nextLineLength = GetVisualLineLength(viewModel, nextLineIndex);

            // Calculate new cursor position
            viewModel.CursorPosition = nextLineStart + long.Min(_desiredColumn, nextLineLength);
        }
        else
        {
            // If the document is empty or at the end of the last line, set cursor to the end of the document
            if (viewModel.TextBuffer.Length == 0)
            {
                viewModel.CursorPosition = 0;
            }
            else
            {
                var lastLineStart = viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
                var lastLineLength = viewModel.TextBuffer.GetLineLength((int)currentLineIndex);
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
            var start = long.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = long.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
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
            var start = long.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = long.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            var length = end - start;

            viewModel.DeleteText(start, length);
            viewModel.CursorPosition = start;
        }

        var insertPosition = viewModel.CursorPosition;
        viewModel.InsertText(insertPosition, "\n");

        // Get the position of the start of the next line
        var nextLineStart =
            viewModel.TextBuffer.GetLineStartPosition(
                (int)viewModel.TextBuffer.GetLineIndexFromPosition((int)insertPosition) + 1);
        viewModel.CursorPosition = nextLineStart;

        // Clear selection after insertion
        viewModel.ClearSelection();
        _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition);

        // Ensure the gutter is updated
        viewModel.NotifyGutterOfLineChange();
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

        if (cursorPosition == 0)
            return;

        var lineIndex = GetLineIndex(viewModel, cursorPosition);
        var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);

        if (cursorPosition == lineStart)
        {
            // If at the start of a line, move to the end of the previous line
            if (lineIndex > 0)
            {
                var previousLineIndex = lineIndex - 1;
                viewModel.CursorPosition = viewModel.TextBuffer.GetLineEndPosition((int)previousLineIndex);
            }

            return;
        }

        var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
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
        var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineEnd = viewModel.TextBuffer.GetLineEndPosition((int)lineIndex);

        if (cursorPosition >= lineEnd)
        {
            // Move to the start of the next line if at the end of the current line
            if (lineIndex < viewModel.TextBuffer.LineCount - 1)
                viewModel.CursorPosition = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex + 1);
            return;
        }

        var lineText = viewModel.TextBuffer.ToString().Substring((int)cursorPosition, (int)(lineEnd - cursorPosition));
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
        if (lineIndex >= viewModel.TextBuffer.LineStarts.Count) UpdateLineCache(lineIndex);

        // Bounds check and calculate _desiredColumn
        if (lineIndex >= 0 && lineIndex < viewModel.TextBuffer.LineStarts.Count)
        {
            var lineStart = viewModel.TextBuffer.LineStarts[(int)lineIndex];
            _desiredColumn = viewModel.CursorPosition - lineStart;
        }
        else
        {
            _desiredColumn = 0;
        }
    }
    
    private long GetVisualLineLength(TextEditorViewModel viewModel, long lineIndex)
    {
        var textBuffer = viewModel.TextBuffer;
        var lineLength = textBuffer.GetLineLength(lineIndex);

        return lineLength;
    }
    
    private long GetLineIndex(TextEditorViewModel viewModel, long position)
    {
        return viewModel.TextBuffer.Rope.GetLineIndexFromPosition((int)position);
    }

    public override void Render(DrawingContext context)
    {
        if (_scrollableViewModel == null) return;

        context.FillRectangle(Brushes.White, new Rect(Bounds.Size));

        var lineCount = GetLineCount();
        if (lineCount == 0) return;

        var viewableAreaWidth = _scrollableViewModel.Viewport.Width + LinePadding;
        var viewableAreaHeight = _scrollableViewModel.Viewport.Height;

        var firstVisibleLine = Math.Max(0,
            _scrollableViewModel.VerticalOffset / LineHeight - 5);
        var lastVisibleLine = long.Min(
            (long)(firstVisibleLine + viewableAreaHeight / LineHeight + 10),
            lineCount);

        // Draw background for the current line
        var viewModel = _scrollableViewModel.TextEditorViewModel;
        RenderCurrentLine(context, viewModel, viewableAreaWidth);
        
        RenderVisibleLines(context, _scrollableViewModel, (long)firstVisibleLine, lastVisibleLine,
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
            var selectionStart = long.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var selectionEnd = long.Max(viewModel.SelectionStart, viewModel.SelectionEnd);

            var lineStartOffset = cursorLine == selectionStartLine
                ? selectionStart - viewModel.TextBuffer.LineStarts[(int)cursorLine]
                : 0;
            var lineEndOffset = cursorLine == selectionEndLine
                ? selectionEnd - viewModel.TextBuffer.LineStarts[(int)cursorLine]
                : GetVisualLineLength(viewModel, cursorLine);

            var xStart = lineStartOffset * viewModel.CharWidth;
            var xEnd = lineEndOffset * viewModel.CharWidth;

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
        long firstVisibleLine, long lastVisibleLine, double viewableAreaWidth)
    {
        const int startIndexBuffer = 5;
        var yOffset = (double)firstVisibleLine * LineHeight;

        for (var i = firstVisibleLine; i < lastVisibleLine; i++)
        {
            var lineText = GetLineText((int)i); // Convert long to int for method call
            var xOffset = 0;

            if (string.IsNullOrEmpty(lineText))
            {
                // Handle empty lines
                yOffset += LineHeight;
                continue;
            }

            // Calculate the start index and the number of characters to display based on the visible area width
            var startIndex = long.Max(0,
                ConvertDoubleTolong(scrollableViewModel.HorizontalOffset /
                                    scrollableViewModel.TextEditorViewModel.CharWidth) - startIndexBuffer);

            // Ensure startIndex is within the lineText length
            if (startIndex >= lineText.Length) startIndex = long.Max(0, lineText.Length - 1);

            var maxCharsToDisplay = long.Min(lineText.Length - startIndex,
                ConvertDoubleTolong((viewableAreaWidth - LinePadding) /
                                    scrollableViewModel.TextEditorViewModel.CharWidth) + startIndexBuffer * 2);

            // Ensure maxCharsToDisplay is non-negative
            if (maxCharsToDisplay < 0) maxCharsToDisplay = 0;

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
                new Point(xOffset + startIndex * scrollableViewModel.TextEditorViewModel.CharWidth,
                    yOffset + verticalOffset));

            yOffset += LineHeight;
        }
    }

    private long ConvertDoubleTolong(double value)
    {
        return (long)Math.Floor(value);
    }

    private double ConvertlongToDouble(long value)
    {
        return (double)value;
    }

    private void DrawSelection(DrawingContext context, double viewableAreaWidth,
        double viewableAreaHeight, ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        if (viewModel.SelectionStart == viewModel.SelectionEnd) return;

        var selectionStart = long.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
        var selectionEnd = long.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
        
        var cursorPosition = viewModel.CursorPosition;

        var startLine = GetLineIndexFromPosition(selectionStart);
        var endLine = GetLineIndexFromPosition(selectionEnd);
        var cursorLine = GetLineIndexFromPosition(cursorPosition);

        var firstVisibleLine = long.Max(0,
            ConvertDoubleTolong(_scrollableViewModel.VerticalOffset / LineHeight) - 5);
        var lastVisibleLine = long.Min(
            firstVisibleLine + ConvertDoubleTolong(viewableAreaHeight / LineHeight) + 1 + 5,
            GetLineCount());

        for (var i = long.Max(startLine, firstVisibleLine); i <= long.Min(endLine, lastVisibleLine); i++)
        {
            var lineStartOffset = i == startLine ? selectionStart - viewModel.TextBuffer.LineStarts[(int)i] : 0;
            var lineEndOffset = i == endLine
                ? selectionEnd - viewModel.TextBuffer.LineStarts[(int)i]
                : GetVisualLineLength(viewModel, i);

            if (i == cursorLine && cursorPosition == selectionEnd)
                lineEndOffset = long.Min(lineEndOffset, cursorPosition - viewModel.TextBuffer.LineStarts[(int)i]);

            var xStart = lineStartOffset * viewModel.CharWidth;
            var xEnd = lineEndOffset * viewModel.CharWidth;
            var y = (double)i * LineHeight;

            // Get the actual line length
            var actualLineLength = GetVisualLineLength(viewModel, i) * viewModel.CharWidth;

            // Skip drawing selection if the line is empty and the cursor is on this line
            if (actualLineLength == 0 && i == cursorLine) continue;

            // Determine if this is the last line of selection
            var isLastSelectionLine = i == endLine;

            // Calculate the selection width, accounting for line length, padding, and empty lines
            var selectionWidth = xEnd - xStart;
            if (actualLineLength == 0) // Empty line
            {
                selectionWidth = viewModel.CharWidth;
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
            selectionWidth = Math.Max(selectionWidth, viewModel.CharWidth);

            var selectionRect = new Rect(xStart, y, selectionWidth, LineHeight);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(100, 139, 205, 192)), selectionRect);
        }
    }

    private void DrawCursor(DrawingContext context, double viewableAreaWidth, double viewableAreaHeight,
        ScrollableTextEditorViewModel scrollableViewModel)
    {
        var viewModel = scrollableViewModel.TextEditorViewModel;

        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var lineStartPosition = viewModel.TextBuffer.LineStarts[(int)cursorLine];
        var cursorColumn = viewModel.CursorPosition - lineStartPosition;

        // Calculate cursor position relative to the visible area
        var cursorXRelative = cursorColumn * viewModel.CharWidth;

        // Use the helper method to convert long to double for the calculation
        var cursorY = (double)cursorLine * LineHeight;

        if (cursorXRelative >= 0)
            context.DrawLine(
                new Pen(Brushes.Black),
                new Point(cursorXRelative, cursorY),
                new Point(cursorXRelative, cursorY + LineHeight) // Use fixed LineHeight
            );
    }

    private long GetLineIndexFromPosition(long position)
    {
        return _scrollableViewModel.TextEditorViewModel.TextBuffer.Rope.GetLineIndexFromPosition((int)position);
    }

    private void SelectAll()
    {
        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            viewModel.SelectionStart = 0;
            viewModel.SelectionEnd = viewModel.TextBuffer.Length;
            viewModel.CursorPosition = viewModel.TextBuffer.Length;
            _selectionAnchor = 0;
            _lastKnownSelection = (0, viewModel.TextBuffer.Length);
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

            // Get text directly from the Rope, considering line breaks
            var selectedText =
                viewModel.TextBuffer.Rope.GetText((int)selectionStart, (int)(selectionEnd - selectionStart));

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

            Dispatcher.UIThread.InvokeAsync(async () => 
            {
                // Disable updates while pasting
                viewModel.ShouldScrollToCursor = false;
                if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
                {
                    var start = long.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
                    var length = long.Abs(viewModel.SelectionEnd - viewModel.SelectionStart);
                    viewModel.DeleteText(start, length);
                    OnTextDeleted(lineIndex, length);
                    insertPosition = start;
                }

                viewModel.InsertText(insertPosition, text);
                OnTextInserted(lineIndex, text.Length);
                viewModel.CursorPosition += text.Length;
                viewModel.CursorPosition = long.Min(viewModel.CursorPosition, viewModel.TextBuffer.Length);
                UpdateDesiredColumn(viewModel);

                viewModel.UpdateLineStarts();
                viewModel.ClearSelection();
                _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition);

                InvalidateVisual();

                await Task.Delay(50);
                viewModel.ShouldScrollToCursor = true;
                UpdateHorizontalScrollPosition();
                EnsureCursorVisible();
            });
        }
    }

    private void UpdateHorizontalScrollPosition()
    {
        if (_scrollableViewModel == null ||
            _scrollableViewModel.TextEditorViewModel.ShouldScrollToCursor == false) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var cursorColumn = viewModel.CursorPosition - viewModel.TextBuffer.LineStarts[(int)cursorLine];
        var cursorX = cursorColumn * viewModel.CharWidth;

        if (cursorX < _scrollableViewModel.HorizontalOffset)
            _scrollableViewModel.HorizontalOffset = cursorX;
        else if (cursorX > _scrollableViewModel.HorizontalOffset + _scrollableViewModel.Viewport.Width)
            _scrollableViewModel.HorizontalOffset =
                cursorX - _scrollableViewModel.Viewport.Width + _scrollableViewModel.TextEditorViewModel.CharWidth;
    }
}
