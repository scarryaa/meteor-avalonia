using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.ViewModels;
using ReactiveUI;

[assembly: InternalsVisibleTo("tests")]

namespace meteor.Views;


public partial class TextEditor : UserControl
{
    private const double DefaultFontSize = 13;
    private const double BaseLineHeight = 20;
    private const double SelectionEndPadding = 2;
    private const double LinePadding = 20;
    private readonly double _lineSpacingFactor = BaseLineHeight / DefaultFontSize;
    private readonly Dictionary<long, long> _lineLengths = new();
    private readonly HashSet<char> _commonCodingSymbols = new("(){}[]<>.,;:'\"\\|`~!@#$%^&*-+=/?");
    
    private double _fontSize = DefaultFontSize;
    private double _lineHeight = BaseLineHeight;
    private bool _suppressScrollOnNextCursorMove;
    private bool _isSelecting;
    private long _selectionAnchor = -1;
    private long _desiredColumn;
    private long _longestLineLength;
    private ScrollableTextEditorViewModel? _scrollableViewModel;
    private (long Start, long End) _lastKnownSelection = (-1, -1);

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<TextEditor, FontFamily>(nameof(FontFamily),
            new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono"));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TextEditor, double>(nameof(FontSize), DefaultFontSize);

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<TextEditor, double>(nameof(LineHeight), BaseLineHeight);

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        init => SetValue(FontFamilyProperty, value);
    }

    public double FontSize => GetValue(FontSizeProperty);

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

        this.GetObservable(FontFamilyProperty).Subscribe(OnFontFamilyChanged);
        this.GetObservable(FontSizeProperty).Subscribe(OnFontSizeChanged);
        this.GetObservable(LineHeightProperty).Subscribe(OnLineHeightChanged);
        
        MeasureCharWidth();
        UpdateLineCache(-1);
    }

    public void UpdateHeight(double height)
    {
        Height = height;
    }

    internal void OnTextChanged(long lineIndex)
    {
        _scrollableViewModel.TextEditorViewModel.LineCache.InvalidateLine(lineIndex);
        InvalidateVisual();
        _scrollableViewModel?.TextEditorViewModel.NotifyGutterOfLineChange();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_scrollableViewModel?.TextEditorViewModel != null)
            _scrollableViewModel.TextEditorViewModel.PropertyChanged -= ViewModel_PropertyChanged;

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            _scrollableViewModel = scrollableViewModel;
            var viewModel = scrollableViewModel.TextEditorViewModel;
            viewModel.LineHeight = LineHeight;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _scrollableViewModel.TextEditorViewModel.RequestFocus += OnRequestFocus;

            viewModel.InvalidateRequired += OnInvalidateRequired;
            Bind(LineHeightProperty, viewModel.WhenAnyValue(vm => vm.LineHeight));

            UpdateLineCache(-1);
        }
    }

    private void OnRequestFocus(object? sender, EventArgs e)
    {
        Focus();
    }

    private void OnInvalidateRequired(object? sender, EventArgs e)
    {
        InvalidateVisual();
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

        if (_scrollableViewModel != null)
            _scrollableViewModel.LongestLineWidth =
                ConvertLongToDouble(_longestLineLength) * _scrollableViewModel.TextEditorViewModel.CharWidth +
                LinePadding;
    }

    private void UpdateMetrics()
    {
        MeasureCharWidth();
        LineHeight = Math.Ceiling(_fontSize * _lineSpacingFactor);
    }

    public void UpdateLineCache(long changedLineIndex, int linesInserted = 0)
    {
        _scrollableViewModel?.TextEditorViewModel?.TextBuffer?.UpdateLineCache();
        InvalidateVisual();
    }

    private long GetLineCount()
    {
        return _scrollableViewModel?.TextEditorViewModel.TextBuffer.LineCount ?? 0;
    }

    private string GetLineText(long lineIndex)
    {
        return _scrollableViewModel.TextEditorViewModel.LineCache.GetLine(lineIndex, index =>
        {
            if (_scrollableViewModel == null)
                return string.Empty;

            var viewModel = _scrollableViewModel.TextEditorViewModel;
            if (lineIndex < 0 || lineIndex >= viewModel.TextBuffer.LineCount)
                return string.Empty;

            return viewModel.TextBuffer.GetLineText((int)lineIndex);
        });
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TextEditorViewModel.TextBuffer))
        {
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
        if (_scrollableViewModel?.TextEditorViewModel == null) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);

        if (cursorLine < 0 || cursorLine >= viewModel.TextBuffer.LineStarts.Count)
            // Invalid cursor line index, return to avoid out-of-range access
            return;

        var cursorColumn = viewModel.CursorPosition - viewModel.TextBuffer.LineStarts[(int)cursorLine];

        if (!_suppressScrollOnNextCursorMove)
        {
            AdjustVerticalScroll(cursorLine);
            AdjustHorizontalScroll(cursorColumn);
        }

        _suppressScrollOnNextCursorMove = false;
        InvalidateVisual();
    }

    private void AdjustVerticalScroll(long cursorLine)
    {
        var cursorY = cursorLine * LineHeight;
        var bottomPadding = 5;
        var verticalBufferLines = 0;
        var verticalBufferHeight = verticalBufferLines * LineHeight;

        if (cursorY < _scrollableViewModel!.VerticalOffset + verticalBufferHeight)
            _scrollableViewModel.VerticalOffset = Math.Max(0, cursorY - verticalBufferHeight);
        else if (cursorY + LineHeight + bottomPadding > _scrollableViewModel.VerticalOffset +
                 _scrollableViewModel.Viewport.Height - verticalBufferHeight)
            _scrollableViewModel.VerticalOffset = cursorY + LineHeight + bottomPadding -
                _scrollableViewModel.Viewport.Height + verticalBufferHeight;
    }

    private void AdjustHorizontalScroll(long cursorColumn)
    {
        if (_scrollableViewModel!.DisableHorizontalScrollToCursor)
        {
            _lastKnownSelection = (_scrollableViewModel.TextEditorViewModel.SelectionStart,
                _scrollableViewModel.TextEditorViewModel.SelectionEnd);
            return;
        }

        var cursorX = cursorColumn * _scrollableViewModel.TextEditorViewModel.CharWidth;
        var viewportWidth = _scrollableViewModel.Viewport.Width;
        var currentOffset = _scrollableViewModel.HorizontalOffset;

        var margin = viewportWidth * 0.1;

        if (cursorX < currentOffset + margin)
            _scrollableViewModel.HorizontalOffset = Math.Max(0, cursorX - margin);
        else if (cursorX > currentOffset + viewportWidth - margin)
            _scrollableViewModel.HorizontalOffset = Math.Max(0, cursorX - viewportWidth + margin);
    }

    private long GetPositionFromPoint(Point point)
    {
        if (_scrollableViewModel == null)
            return 0;

        var lineIndex = (long)(point.Y / LineHeight);
        var column = (long)(point.X / _scrollableViewModel.TextEditorViewModel.CharWidth);

        lineIndex = Math.Max(0, Math.Min(lineIndex, GetLineCount() - 1));
        var lineStart = _scrollableViewModel.TextEditorViewModel.TextBuffer.LineStarts[(int)lineIndex];
        var lineLength = GetVisualLineLength(_scrollableViewModel.TextEditorViewModel, lineIndex);
        column = Math.Max(0, Math.Min(column, lineLength));

        return lineStart + column;
    }


    private void HandleTextInsertion(long position, string text)
    {
        var lineIndex = GetLineIndex(_scrollableViewModel.TextEditorViewModel, position);
        OnTextChanged(lineIndex);

        // If the inserted text contains newlines, invalidate all subsequent lines
        if (text.Contains('\n'))
            for (var i = lineIndex + 1; i < GetLineCount(); i++)
                OnTextChanged(i);
    }

    private void HandleTextDeletion(long position, long length)
    {
        var startLineIndex = GetLineIndex(_scrollableViewModel.TextEditorViewModel, position);
        var endLineIndex = GetLineIndex(_scrollableViewModel.TextEditorViewModel, position + length);

        for (var i = startLineIndex; i <= endLineIndex; i++) OnTextChanged(i);
    }


    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (_scrollableViewModel != null && !string.IsNullOrEmpty(e.Text))
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var insertPosition = viewModel.CursorPosition;

            if (_lastKnownSelection.Start != _lastKnownSelection.End)
            {
                var start = Math.Min(_lastKnownSelection.Start, _lastKnownSelection.End);
                var end = Math.Max(_lastKnownSelection.Start, _lastKnownSelection.End);
                var length = end - start;

                viewModel.DeleteText(start, length);
                insertPosition = start;
            }

            var currentLineIndex = GetLineIndex(viewModel, insertPosition);
            viewModel.InsertText(insertPosition, e.Text);
            HandleTextInsertion(insertPosition, e.Text);

            viewModel.CursorPosition = insertPosition + e.Text.Length;
            viewModel.SelectionStart = viewModel.CursorPosition;
            viewModel.SelectionEnd = viewModel.CursorPosition;
            viewModel.IsSelecting = false;

            _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition);
            _selectionAnchor = -1;
        }

        InvalidateVisual();
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

            // Handle Ctrl+Z and Ctrl+Shift+Z
            if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    // Ctrl+Shift+Z: Redo
                    _scrollableViewModel.TabViewModel.Redo();
                else
                    // Ctrl+Z: Undo
                    _scrollableViewModel.TabViewModel.Undo();

                viewModel.TextBuffer.UpdateLineCache();
                UpdateLineCache(-1);

                // Invalidate visuals to ensure proper redraw
                viewModel.LineCache.Clear();
                InvalidateVisual();

                // Ensure the cursor is visible
                EnsureCursorVisible();

                e.Handled = true;
                return;
            }

            HandleKeyDown(e, viewModel);

            var insertPosition = viewModel.CursorPosition;
            HandleTextInsertion(insertPosition, e.ToString());
            InvalidateVisual();
        }
    }

    private void HandleKeyDown(KeyEventArgs e, TextEditorViewModel viewModel)
    {
        _suppressScrollOnNextCursorMove = false;
        var shiftFlag = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        var ctrlFlag = (e.KeyModifiers & KeyModifiers.Control) != 0;

        if (e.Key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.CapsLock)
            return;

        if (ctrlFlag)
        {
            HandleControlKeyDown(e, viewModel);
            return;
        }

        if (shiftFlag && _selectionAnchor == -1 && e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            _selectionAnchor = viewModel.SelectionStart == 0 && viewModel.SelectionEnd == viewModel.TextBuffer.Length
                ? e.Key switch
                {
                    Key.Left => viewModel.SelectionEnd,
                    Key.Right => viewModel.SelectionStart,
                    Key.Up => viewModel.SelectionEnd,
                    Key.Down => viewModel.SelectionStart,
                    _ => viewModel.CursorPosition
                }
                : viewModel.CursorPosition;
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
                HandleLeftArrow(viewModel, shiftFlag);
                break;
            case Key.Right:
                HandleRightArrow(viewModel, shiftFlag);
                break;
            case Key.Up:
                HandleUpArrow(viewModel, shiftFlag);
                break;
            case Key.Down:
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

        if (shiftFlag && e.Key.ToString().Length != 1)
        {
            UpdateSelection(viewModel);
        }
        else
        {
            viewModel.ClearSelection();
            _selectionAnchor = -1;
        }

        viewModel.CursorPosition = Math.Min(Math.Max(viewModel.CursorPosition, 0), viewModel.TextBuffer.Length);
        InvalidateVisual();
    }

    private void HandleControlKeyDown(KeyEventArgs e, TextEditorViewModel viewModel)
    {
        var shiftFlag = (e.KeyModifiers & KeyModifiers.Shift) != 0;

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
        if (viewModel.CursorPosition == 0) return;

        var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);

        if (viewModel.CursorPosition == lineStart)
        {
            if (lineIndex > 0)
            {
                viewModel.CursorPosition = viewModel.TextBuffer.GetLineEndPosition((int)(lineIndex - 1));
            }
            return;
        }

        var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
        var index = (int)(viewModel.CursorPosition - lineStart - 1);

        while (index > 0 && char.IsWhiteSpace(lineText[index])) index--;

        if (index > 0)
        {
            if (IsCommonCodingSymbol(lineText[index]))
                while (index > 0 && IsCommonCodingSymbol(lineText[index - 1]))
                    index--;
            else
                while (index > 0 && !char.IsWhiteSpace(lineText[index - 1]) &&
                       !IsCommonCodingSymbol(lineText[index - 1]))
                    index--;
        }

        viewModel.CursorPosition = lineStart + index;
        UpdateSelectionAfterCursorMove(viewModel, extendSelection);
    }

    private void MoveCursorToNextWord(TextEditorViewModel viewModel, bool extendSelection)
    {
        var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineEnd = viewModel.TextBuffer.GetLineEndPosition((int)lineIndex);

        if (viewModel.CursorPosition >= lineEnd)
        {
            if (lineIndex < viewModel.TextBuffer.LineCount - 1)
                viewModel.CursorPosition = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex + 1);
            return;
        }

        var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
        var index = (int)(viewModel.CursorPosition - lineStart);

        while (index < lineText.Length && char.IsWhiteSpace(lineText[index])) index++;

        if (index < lineText.Length)
        {
            if (IsCommonCodingSymbol(lineText[index]))
                while (index < lineText.Length && IsCommonCodingSymbol(lineText[index]))
                    index++;
            else
                while (index < lineText.Length && !char.IsWhiteSpace(lineText[index]) &&
                       !IsCommonCodingSymbol(lineText[index]))
                    index++;
        }

        viewModel.CursorPosition = lineStart + index;
        UpdateSelectionAfterCursorMove(viewModel, extendSelection);
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
            HandleTextDeletion(start, length);

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
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var lineStartPosition = viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
        var lineLength = GetVisualLineLength(viewModel, currentLineIndex);
        viewModel.CursorPosition = lineStartPosition + lineLength;
        UpdateDesiredColumn(viewModel);
        if (!isShiftPressed) viewModel.ClearSelection();
    }

    private void HandleHome(TextEditorViewModel viewModel, bool isShiftPressed)
    {
        var currentLineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var lineStartPosition = viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
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
            {
                viewModel.SelectionEnd = viewModel.CursorPosition;
            }
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
            {
                viewModel.SelectionEnd = viewModel.CursorPosition;
            }
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
            HandleTextDeletion(start, length);

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
        HandleTextInsertion(insertPosition, "\n");

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

    private void UpdateSelectionAfterCursorMove(TextEditorViewModel viewModel, bool extendSelection)
    {
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
    }

    private bool IsCommonCodingSymbol(char c)
    {
        return _commonCodingSymbols.Contains(c);
    }

    private void ScrollViewport(double delta)
    {
        if (_scrollableViewModel != null)
        {
            var newOffset = _scrollableViewModel.VerticalOffset + delta;
            var maxOffset = GetLineCount() * LineHeight - _scrollableViewModel.Viewport.Height;
            _scrollableViewModel.VerticalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));
        }
    }

    private void UpdateDesiredColumn(TextEditorViewModel viewModel)
    {
        var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);

        if (lineIndex >= viewModel.TextBuffer.LineStarts.Count) UpdateLineCache(lineIndex);

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
        return viewModel.TextBuffer.GetLineLength(lineIndex);
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

        var firstVisibleLine = Math.Max(0, (long)(_scrollableViewModel.VerticalOffset / LineHeight) - 5);
        var lastVisibleLine = Math.Min(firstVisibleLine + (long)(viewableAreaHeight / LineHeight) + 11, lineCount);

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        RenderCurrentLine(context, viewModel, viewableAreaWidth);

        RenderVisibleLines(context, _scrollableViewModel, firstVisibleLine, lastVisibleLine, viewableAreaWidth);
        DrawSelection(context, viewableAreaWidth, viewableAreaHeight, _scrollableViewModel);
        DrawCursor(context, viewableAreaWidth, viewableAreaHeight, _scrollableViewModel);
    }

    private void RenderCurrentLine(DrawingContext context, TextEditorViewModel viewModel, double viewableAreaWidth)
    {
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var y = cursorLine * LineHeight;

        var selectionStartLine = GetLineIndexFromPosition(viewModel.SelectionStart);
        var selectionEndLine = GetLineIndexFromPosition(viewModel.SelectionEnd);

        // Calculate the total width including horizontal offset
        var totalWidth = Math.Max(viewModel.WindowWidth, viewableAreaWidth + _scrollableViewModel!.HorizontalOffset);

        // Check if the cursor line is outside the selection range
        if (cursorLine < selectionStartLine || cursorLine > selectionEndLine)
        {
            var rect = new Rect(0, y, totalWidth, LineHeight);
            context.FillRectangle(new SolidColorBrush(Color.Parse("#ededed")), rect);
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
                : GetVisualLineLength(viewModel, cursorLine);

            var xStart = lineStartOffset * viewModel.CharWidth;
            var xEnd = lineEndOffset * viewModel.CharWidth;

            // Highlight the area before the selection
            if (xStart > 0)
            {
                var beforeSelectionRect = new Rect(0, y, xStart, LineHeight);
                context.FillRectangle(new SolidColorBrush(Color.Parse("#ededed")), beforeSelectionRect);
            }

            // Highlight the area after the selection
            var afterSelectionRect = new Rect(xEnd, y, totalWidth - xEnd, LineHeight);
            context.FillRectangle(new SolidColorBrush(Color.Parse("#ededed")), afterSelectionRect);
        }
    }

    private void RenderVisibleLines(DrawingContext context, ScrollableTextEditorViewModel scrollableViewModel,
        long firstVisibleLine, long lastVisibleLine, double viewableAreaWidth)
    {
        const int startIndexBuffer = 5;
        var yOffset = firstVisibleLine * LineHeight;

        for (var i = firstVisibleLine; i < lastVisibleLine; i++)
        {
            var lineText = GetLineText((int)i);
            if (string.IsNullOrEmpty(lineText))
            {
                yOffset += LineHeight;
                continue;
            }

            var startIndex = Math.Max(0,
                (long)(scrollableViewModel.HorizontalOffset / scrollableViewModel.TextEditorViewModel.CharWidth) -
                startIndexBuffer);
            startIndex = Math.Min(startIndex, lineText.Length - 1);

            var maxCharsToDisplay = Math.Min(lineText.Length - startIndex,
                (long)((viewableAreaWidth - LinePadding) / scrollableViewModel.TextEditorViewModel.CharWidth) +
                startIndexBuffer * 2);
            maxCharsToDisplay = Math.Max(0, maxCharsToDisplay);

            var visiblePart = lineText.Substring((int)startIndex, (int)maxCharsToDisplay);

            var formattedText = new FormattedText(
                visiblePart,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily),
                FontSize,
                Brushes.Black);

            var verticalOffset = (LineHeight - formattedText.Height) / 2;

            context.DrawText(formattedText,
                new Point(startIndex * scrollableViewModel.TextEditorViewModel.CharWidth,
                    yOffset + verticalOffset));

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

        var firstVisibleLine = Math.Max(0, (long)(scrollableViewModel.VerticalOffset / LineHeight) - 5);
        var lastVisibleLine = Math.Min(firstVisibleLine + (long)(viewableAreaHeight / LineHeight) + 11, GetLineCount());

        for (var i = Math.Max(startLine, firstVisibleLine); i <= Math.Min(endLine, lastVisibleLine); i++)
        {
            var lineStartOffset = i == startLine ? selectionStart - viewModel.TextBuffer.LineStarts[(int)i] : 0;
            var lineEndOffset = i == endLine
                ? selectionEnd - viewModel.TextBuffer.LineStarts[(int)i]
                : GetVisualLineLength(viewModel, i);

            if (i == cursorLine && cursorPosition == selectionEnd)
                lineEndOffset = Math.Min(lineEndOffset, cursorPosition - viewModel.TextBuffer.LineStarts[(int)i]);

            var xStart = lineStartOffset * viewModel.CharWidth;
            var xEnd = lineEndOffset * viewModel.CharWidth;
            var y = i * LineHeight;

            var actualLineLength = GetVisualLineLength(viewModel, i) * viewModel.CharWidth;

            if (actualLineLength == 0 && i == cursorLine) continue;

            var isLastSelectionLine = i == endLine;

            var selectionWidth = xEnd - xStart;
            if (actualLineLength == 0)
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

        var cursorXRelative = cursorColumn * viewModel.CharWidth;
        var cursorY = cursorLine * LineHeight;

        if (cursorXRelative >= 0)
            context.DrawLine(
                new Pen(Brushes.Black),
                new Point(cursorXRelative, cursorY),
                new Point(cursorXRelative, cursorY + LineHeight)
            );
    }

    private long GetLineIndexFromPosition(long position)
    {
        return _scrollableViewModel.TextEditorViewModel.TextBuffer.Rope.GetLineIndexFromPosition((int)position);
    }

    internal void SelectAll()
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

    internal async Task CopyText()
    {
        if (_scrollableViewModel?.TextEditorViewModel == null) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        if (viewModel.SelectionStart == -1 || viewModel.SelectionEnd == -1) return;

        var selectionStart = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
        var selectionEnd = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);

        var selectedText = viewModel.TextBuffer.Rope.GetText((int)selectionStart, (int)(selectionEnd - selectionStart));

        await _scrollableViewModel.ClipboardService.SetTextAsync(selectedText);
    }

    internal async Task PasteText()
    {
        var viewModel = _scrollableViewModel!.TextEditorViewModel;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        var text = await clipboard.GetTextAsync();
        if (string.IsNullOrEmpty(text)) return;

        await Dispatcher.UIThread.InvokeAsync(async () => 
        {
            var insertPosition = viewModel.CursorPosition;

            // Handle selection deletion in a single operation
            if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
            {
                var start = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
                var length = Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart);
                viewModel.DeleteText(start, length);
                insertPosition = start;
            }

            // Perform text insertion as a single operation
            viewModel.InsertText(insertPosition, text);
            HandleTextInsertion(insertPosition, text);
            
            // Update only affected lines
            viewModel.UpdateLineStarts();

            viewModel.CursorPosition = insertPosition + text.Length;
            UpdateDesiredColumn(viewModel);

            viewModel.ClearSelection();
            _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition);

            // Defer UI updates
            await Task.Delay(10);
            InvalidateVisual();

            await Task.Delay(50);
            UpdateHorizontalScrollPosition();
            EnsureCursorVisible();
        }, DispatcherPriority.Background);
    }

    private void UpdateHorizontalScrollPosition()
    {
        if (_scrollableViewModel?.TextEditorViewModel.ShouldScrollToCursor != true) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);
        var cursorColumn = viewModel.CursorPosition - viewModel.TextBuffer.LineStarts[(int)cursorLine];
        var cursorX = cursorColumn * viewModel.CharWidth;

        if (cursorX < _scrollableViewModel.HorizontalOffset)
            _scrollableViewModel.HorizontalOffset = cursorX;
        else if (cursorX > _scrollableViewModel.HorizontalOffset + _scrollableViewModel.Viewport.Width)
            _scrollableViewModel.HorizontalOffset = cursorX - _scrollableViewModel.Viewport.Width + viewModel.CharWidth;
    }

    private long ConvertDoubleToLong(double value)
    {
        return (long)Math.Floor(value);
    }

    private double ConvertLongToDouble(long value)
    {
        return value;
    }
}
