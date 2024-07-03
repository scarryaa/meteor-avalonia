using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.ViewModels;
using SelectionChangedEventArgs = meteor.Models.SelectionChangedEventArgs;
using WhenAnyMixin = ReactiveUI.WhenAnyMixin;

[assembly: InternalsVisibleTo("tests")]

namespace meteor.Views;

public partial class TextEditor : UserControl
{
    private bool _isDoubleClickDrag;
    private bool _isTripleClickDrag;
    private readonly object _tabOperationLock = new();
    private bool _isTabOperationInProgress;
    private const int TabSize = 4;
    private readonly bool UseTabCharacter = false;
    private const double HorizontalScrollThreshold = 20;
    private const double VerticalScrollThreshold = 20;
    private const double ScrollSpeed = 1;
    private const double ScrollAcceleration = 1.05;

    private const int DoubleClickTimeThreshold = 300;
    private const int TripleClickTimeThreshold = 600;
    private const double DoubleClickDistanceThreshold = 5;
    private const double DefaultFontSize = 13;
    private const double BaseLineHeight = 20;
    private const double SelectionEndPadding = 2;
    private const double LinePadding = 20;

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<TextEditor, double>(nameof(LineHeight), BaseLineHeight);

    public static readonly StyledProperty<IBrush> BackgroundBrushProperty =
        AvaloniaProperty.Register<TextEditor, IBrush>(nameof(BackgroundBrush), Brushes.White);

    public static readonly StyledProperty<IBrush> CursorBrushProperty =
        AvaloniaProperty.Register<TextEditor, IBrush>(nameof(CursorBrush), Brushes.Black);

    public static readonly StyledProperty<IBrush> SelectionBrushProperty =
        AvaloniaProperty.Register<TextEditor, IBrush>(nameof(SelectionBrush),
            new SolidColorBrush(Color.FromArgb(100, 139, 205, 192)));

    public static readonly StyledProperty<IBrush> LineHighlightBrushProperty =
        AvaloniaProperty.Register<TextEditor, IBrush>(nameof(LineHighlightBrush),
            new SolidColorBrush(Color.Parse("#ededed")));

    public new static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<TextEditor, FontFamily>(
            nameof(FontFamily),
            new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono"));

    public new static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TextEditor, double>(
            nameof(FontSize),
            13);

    private readonly HashSet<char> _commonCodingSymbols = new("(){}[]<>.,;:'\"\\|`~!@#$%^&*-+=/?");
    private readonly Dictionary<long, long> _lineLengths = new();
    private readonly double _lineSpacingFactor = BaseLineHeight / DefaultFontSize;
    private readonly DispatcherTimer _scrollTimer;
    private double _currentScrollSpeed = ScrollSpeed;
    private long _desiredColumn;

    private double _fontSize = DefaultFontSize;
    private bool _isManualScrolling;
    private bool _isSelecting;
    private Point _lastClickPosition;
    private DateTime _lastClickTime;
    private (long Start, long End) _lastKnownSelection = (-1, -1);
    private double _lineHeight = BaseLineHeight;
    private long _longestLineLength;
    private ScrollableTextEditorViewModel? _scrollableViewModel;
    private long _selectionAnchor = -1;
    private bool _suppressScrollOnNextCursorMove;

    public TextEditor()
    {
        InitializeComponent();
        Cursor = new Cursor(StandardCursorType.Ibeam);
        DataContextChanged += OnDataContextChanged;
        Focusable = true;

        this.GetObservable(FontFamilyProperty).Subscribe(OnFontFamilyChanged);
        this.GetObservable(FontSizeProperty).Subscribe(OnFontSizeChanged);
        this.GetObservable(LineHeightProperty).Subscribe(OnLineHeightChanged);
        this.GetObservable(BackgroundBrushProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(CursorBrushProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(SelectionBrushProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(LineHighlightBrushProperty).Subscribe(_ => InvalidateVisual());

        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);

        MeasureCharWidth();
        UpdateLineCache(-1);

        _scrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _scrollTimer.Tick += ScrollTimer_Tick;
    }

    public new FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public new double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public IBrush BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    public IBrush CursorBrush
    {
        get => GetValue(CursorBrushProperty);
        set => SetValue(CursorBrushProperty, value);
    }

    public IBrush SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    public IBrush LineHighlightBrush
    {
        get => GetValue(LineHighlightBrushProperty);
        set => SetValue(LineHighlightBrushProperty, value);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_scrollableViewModel == null) return;

        _isManualScrolling = true;
        var delta = e.Delta.Y * 3;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            // Horizontal scrolling (shift + scroll)
            var newOffset = _scrollableViewModel.HorizontalOffset -
                            delta * _scrollableViewModel.TextEditorViewModel.CharWidth;
            var maxOffset = Math.Max(0, _scrollableViewModel.LongestLineWidth - _scrollableViewModel.Viewport.Width);
            _scrollableViewModel.HorizontalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));
        }
        else
        {
            // Vertical scrolling
            var newOffset = _scrollableViewModel.VerticalOffset - delta * LineHeight;
            var maxOffset = Math.Max(0, GetLineCount() * LineHeight - _scrollableViewModel.Viewport.Height + 6);
            _scrollableViewModel.VerticalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));

            if (_scrollableViewModel.TextEditorViewModel.IsSelecting)
            {
                // Update selection based on new scroll position
                var position = GetPositionFromPoint(e.GetPosition(this));
                UpdateSelectionDuringManualScroll(position);
            }
        }

        e.Handled = true;
        InvalidateVisual();

        // Use a dispatcher to reset the manual scrolling flag after a short delay
        Dispatcher.UIThread.Post(() => _isManualScrolling = false, DispatcherPriority.Background);
    }

    private void UpdateSelectionDuringManualScroll(long position)
    {
        var viewModel = _scrollableViewModel.TextEditorViewModel;

        if (_isTripleClickDrag)
            UpdateTripleClickSelection(viewModel, position);
        else if (_isDoubleClickDrag)
            UpdateDoubleClickSelection(viewModel, position);
        else
            UpdateNormalSelection(viewModel, position);

        _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
    }

    private void UpdateTripleClickSelection(TextEditorViewModel viewModel, long position)
    {
        var currentLineIndex = GetLineIndex(viewModel, position);
        var anchorLineIndex = GetLineIndex(viewModel, _selectionAnchor);

        var currentLineStart = viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
        var currentLineEnd = currentLineStart + GetVisualLineLength(viewModel, currentLineIndex);

        var anchorLineStart = viewModel.TextBuffer.GetLineStartPosition((int)anchorLineIndex);
        var anchorLineEnd = anchorLineStart + GetVisualLineLength(viewModel, anchorLineIndex);

        if (currentLineIndex < anchorLineIndex)
        {
            viewModel.SelectionStart = currentLineStart;
            viewModel.SelectionEnd = anchorLineEnd;
            viewModel.CursorPosition = currentLineStart;
        }
        else
        {
            viewModel.SelectionStart = anchorLineStart;
            viewModel.SelectionEnd = currentLineEnd;
            viewModel.CursorPosition = currentLineEnd;
        }
    }

    private void UpdateDoubleClickSelection(TextEditorViewModel viewModel, long position)
    {
        var (currentWordStart, currentWordEnd) = FindWordOrSymbolBoundaries(viewModel, position);
        var (anchorWordStart, anchorWordEnd) = FindWordOrSymbolBoundaries(viewModel, _selectionAnchor);

        if (position < _selectionAnchor)
        {
            viewModel.SelectionStart = Math.Min(currentWordStart, anchorWordStart);
            viewModel.SelectionEnd = Math.Max(anchorWordEnd, _selectionAnchor);
            viewModel.CursorPosition = currentWordStart;
        }
        else
        {
            viewModel.SelectionStart = Math.Min(anchorWordStart, _selectionAnchor);
            viewModel.SelectionEnd = Math.Max(currentWordEnd, anchorWordEnd);
            viewModel.CursorPosition = currentWordEnd;
        }
    }

    private void UpdateNormalSelection(TextEditorViewModel viewModel, long position)
    {
        viewModel.CursorPosition = position;
        if (position < _selectionAnchor)
        {
            viewModel.SelectionStart = position;
            viewModel.SelectionEnd = _selectionAnchor;
        }
        else
        {
            viewModel.SelectionStart = _selectionAnchor;
            viewModel.SelectionEnd = position;
        }
    }

    private void ScrollTimer_Tick(object sender, EventArgs e)
    {
        if (_scrollableViewModel == null || _isManualScrolling) return;

        var cursorPosition = _scrollableViewModel.TextEditorViewModel.CursorPosition;
        var cursorPoint = GetPointFromPosition(cursorPosition);

        CheckAndScrollHorizontally(cursorPoint.X, HorizontalScrollThreshold);
        CheckAndScrollVertically(cursorPoint.Y, VerticalScrollThreshold);

        _currentScrollSpeed *= ScrollAcceleration;
        InvalidateVisual();
    }

    private (long start, long end) FindWordOrSymbolBoundaries(TextEditorViewModel viewModel, long position)
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

    private Point GetPointFromPosition(long position)
    {
        if (_scrollableViewModel == null)
            return new Point(0, 0);

        var lineIndex = GetLineIndex(_scrollableViewModel.TextEditorViewModel, position);
        var lineStart = _scrollableViewModel.TextEditorViewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var column = position - lineStart;

        var x = column * _scrollableViewModel.TextEditorViewModel.CharWidth - _scrollableViewModel.HorizontalOffset;
        var y = lineIndex * LineHeight - _scrollableViewModel.VerticalOffset;

        return new Point(x, y);
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
            _scrollableViewModel.TextEditorViewModel.SelectionChanged += OnSelectionChanged;

            viewModel.InvalidateRequired += OnInvalidateRequired;
            Bind(LineHeightProperty, WhenAnyMixin.WhenAnyValue(viewModel, vm => vm.LineHeight));

            UpdateLineCache(-1);
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.SelectionStart.HasValue && e.SelectionEnd.HasValue)
            _lastKnownSelection = (e.SelectionStart.Value, e.SelectionEnd.Value);
        else if (e.SelectionStart.HasValue)
            _lastKnownSelection = (e.SelectionStart.Value, _lastKnownSelection.End);
        else if (e.SelectionEnd.HasValue) _lastKnownSelection = (_lastKnownSelection.Start, e.SelectionEnd.Value);
        InvalidateVisual();
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
            Dispatcher.UIThread.Post(InvalidateVisual);
        else if (e.PropertyName == nameof(TextEditorViewModel.CursorPosition))
            if (_scrollableViewModel.TextEditorViewModel.ShouldScrollToCursor)
                Dispatcher.UIThread.Post(EnsureCursorVisible);
    }

    private double CalculateScrollAmount(double distanceFromEdge, double threshold)
    {
        return Math.Min(_currentScrollSpeed * (1 - distanceFromEdge / threshold), LineHeight);
    }

    private void CheckAndScrollHorizontally(double cursorX, double threshold)
    {
        if (_scrollableViewModel == null) return;

        var viewportWidth = _scrollableViewModel.Viewport.Width;
        var currentOffset = _scrollableViewModel.HorizontalOffset;

        if (cursorX < threshold)
        {
            // Scroll left
            var newOffset = Math.Max(0, currentOffset - CalculateScrollAmount(cursorX, threshold));
            _scrollableViewModel.HorizontalOffset = newOffset;
        }
        else if (cursorX > viewportWidth - threshold)
        {
            // Scroll right
            var newOffset = currentOffset + CalculateScrollAmount(viewportWidth - cursorX, threshold);
            _scrollableViewModel.HorizontalOffset = newOffset;
        }
        else
        {
            _currentScrollSpeed = ScrollSpeed; // Reset scroll speed when not near edges
        }
    }

    private void CheckAndScrollVertically(double cursorY, double threshold)
    {
        if (_scrollableViewModel == null) return;

        var viewportHeight = _scrollableViewModel.Viewport.Height;
        var currentOffset = _scrollableViewModel.VerticalOffset;
        var lineCount = GetLineCount();

        if (cursorY < threshold)
        {
            // Scroll up
            var scrollAmount = CalculateScrollAmount(cursorY, threshold);
            var newOffset = Math.Max(0, currentOffset - scrollAmount);
            _scrollableViewModel.VerticalOffset = newOffset;

            // Adjust cursor position if necessary
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var firstVisibleLine = (long)(newOffset / LineHeight);
            if (GetLineIndex(viewModel, viewModel.CursorPosition) < firstVisibleLine)
            {
                var newCursorPosition = viewModel.TextBuffer.GetLineStartPosition((int)firstVisibleLine);
                if (newCursorPosition < viewModel.SelectionStart)
                {
                    viewModel.SelectionStart = newCursorPosition;
                    viewModel.CursorPosition = newCursorPosition;
                }
            }
        }
        else if (cursorY > viewportHeight - threshold)
        {
            // Scroll down
            var scrollAmount = CalculateScrollAmount(viewportHeight - cursorY, threshold);
            var newOffset = currentOffset + scrollAmount;
            var maxOffset = Math.Max(0, lineCount * LineHeight - viewportHeight);
            _scrollableViewModel.VerticalOffset = Math.Min(newOffset, maxOffset);

            // Adjust cursor position if necessary
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var lastVisibleLine = (long)((newOffset + viewportHeight) / LineHeight);
            if (GetLineIndex(viewModel, viewModel.CursorPosition) > lastVisibleLine)
            {
                var newCursorPosition =
                    viewModel.TextBuffer.GetLineEndPosition((int)Math.Min(lastVisibleLine, lineCount - 1));
                if (newCursorPosition > viewModel.SelectionEnd)
                {
                    viewModel.SelectionEnd = newCursorPosition;
                    viewModel.CursorPosition = newCursorPosition;
                }
            }
        }
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var currentPosition = e.GetPosition(this);
        var currentTime = DateTime.Now;

        // Check for triple-click
        if ((currentTime - _lastClickTime).TotalMilliseconds <= TripleClickTimeThreshold &&
            DistanceBetweenPoints(currentPosition, _lastClickPosition) <= DoubleClickDistanceThreshold &&
            (currentTime - _lastClickTime).TotalMilliseconds > DoubleClickTimeThreshold)
        {
            OnTripleClicked(currentPosition);
            _isTripleClickDrag = true;
            e.Handled = true;
            return;
        }

        // Check for double-click
        if ((currentTime - _lastClickTime).TotalMilliseconds <= DoubleClickTimeThreshold &&
            DistanceBetweenPoints(currentPosition, _lastClickPosition) <= DoubleClickDistanceThreshold)
        {
            OnDoubleClicked(currentPosition);
            _isDoubleClickDrag = true;
            e.Handled = true;
            return;
        }

        // Update last click info
        _lastClickPosition = currentPosition;
        _lastClickTime = currentTime;

        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var position = GetPositionFromPoint(currentPosition);

            if (position >= _scrollableViewModel.TextEditorViewModel.TextBuffer.Length)
                position = _scrollableViewModel.TextEditorViewModel.TextBuffer.Length;

            // Update cursor position and start selection
            viewModel.CursorPosition = position;
            _selectionAnchor = position;

            if (!viewModel.IsSelecting)
                viewModel.SelectionStart = viewModel.SelectionEnd = position;
            else
                viewModel.SelectionEnd = position;
            UpdateSelection(viewModel);
            viewModel.IsSelecting = true;
            _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);

            e.Handled = true;
            InvalidateVisual();
        }
    }

    private double DistanceBetweenPoints(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }

    private void OnDoubleClicked(Point position)
    {
        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var cursorPosition = GetPositionFromPoint(position);

            // Adjust cursorPosition if it is beyond the line length
            var lineIndex = GetLineIndex(viewModel, cursorPosition);
            var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
            var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
            var lineLength = GetVisualLineLength(viewModel, lineIndex);

            if (cursorPosition >= lineStart + lineLength)
            {
                // If clicking beyond the end of the line, select the entire line
                SelectTrailingWhitespace(viewModel, lineIndex, lineText, lineStart);
                return;
            }

            var (wordStart, wordEnd) = FindWordOrSymbolBoundaries(viewModel, cursorPosition);

            // Ensure the end position does not exceed the length of the text buffer
            wordEnd = Math.Min(wordEnd, viewModel.TextBuffer.Length);

            // Update selection to encompass the entire word
            viewModel.SelectionStart = wordStart;
            viewModel.SelectionEnd = wordEnd;
            _selectionAnchor = wordStart;
            viewModel.CursorPosition = wordEnd;

            // Check for word end line index to handle edge cases
            var wordEndLineIndex = GetLineIndex(viewModel, wordEnd);
            if (wordEnd == viewModel.TextBuffer.GetLineStartPosition((int)wordEndLineIndex))
            {
                var (_wordStart, _wordEnd) = FindWordOrSymbolBoundaries(viewModel, cursorPosition);
                viewModel.SelectionEnd = _wordEnd - 1;
                viewModel.CursorPosition = _wordEnd - 1;
            }

            viewModel.IsSelecting = true;
            UpdateSelection(viewModel);
            InvalidateVisual();
        }
    }

    private void OnTripleClicked(Point position)
    {
        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var cursorPosition = GetPositionFromPoint(position);

            var lineIndex = GetLineIndex(viewModel, cursorPosition);
            var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
            var lineLength = GetVisualLineLength(viewModel, lineIndex);
            var lineEnd = lineStart + lineLength;

            viewModel.ShouldScrollToCursor = false;
            viewModel.SelectionStart = lineStart;
            viewModel.SelectionEnd = lineEnd;
            _selectionAnchor = lineStart;
            viewModel.CursorPosition = lineEnd;

            viewModel.IsSelecting = true;
            UpdateSelection(viewModel);
            _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
            InvalidateVisual();
        }
    }

    private void SelectTrailingWhitespace(TextEditorViewModel viewModel, long lineIndex, string lineText,
        long lineStart)
    {
        var lastNonWhitespaceIndex = lineText.Length - 1;
        while (lastNonWhitespaceIndex >= 0 && char.IsWhiteSpace(lineText[lastNonWhitespaceIndex]))
            lastNonWhitespaceIndex--;

        var trailingWhitespaceStart = lineStart + lastNonWhitespaceIndex + 1;
        var trailingWhitespaceEnd = lineStart + GetVisualLineLength(viewModel, lineIndex);

        // If there's no trailing whitespace, select the word or symbol at the end of the line
        if (trailingWhitespaceStart == trailingWhitespaceEnd && lastNonWhitespaceIndex >= 0)
        {
            // Find the boundaries of the word or symbol at the end of the line
            var (wordStart, wordEnd) = FindWordOrSymbolBoundaries(viewModel, lineStart + lastNonWhitespaceIndex);
            trailingWhitespaceStart = wordStart;
            trailingWhitespaceEnd = wordEnd;
        }

        viewModel.SelectionStart = trailingWhitespaceStart;
        viewModel.SelectionEnd = trailingWhitespaceEnd;
        viewModel.CursorPosition = trailingWhitespaceEnd;
        _selectionAnchor = trailingWhitespaceStart;

        UpdateSelection(viewModel);
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            if (viewModel.IsSelecting || _isDoubleClickDrag || _isTripleClickDrag)
            {
                var position = GetPositionFromPoint(e.GetPosition(this));

                if (_isTripleClickDrag)
                    HandleTripleClickDrag(viewModel, position);
                else if (_isDoubleClickDrag)
                    HandleDoubleClickDrag(viewModel, position);
                else
                    HandleNormalDrag(viewModel, position);

                _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
                e.Handled = true;

                if (!_isManualScrolling) _scrollTimer.Start();

                InvalidateVisual();
            }
            else
            {
                _scrollTimer.Stop();
                _currentScrollSpeed = ScrollSpeed;
            }
        }
    }

    private void HandleTripleClickDrag(TextEditorViewModel viewModel, long position)
    {
        viewModel.ShouldScrollToCursor = false;
        var currentLineIndex = GetLineIndex(viewModel, position);
        var anchorLineIndex = GetLineIndex(viewModel, _selectionAnchor);

        var currentLineStart = viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
        var currentLineEnd = currentLineStart + GetVisualLineLength(viewModel, currentLineIndex);

        var anchorLineStart = viewModel.TextBuffer.GetLineStartPosition((int)anchorLineIndex);
        var anchorLineEnd = anchorLineStart + GetVisualLineLength(viewModel, anchorLineIndex);

        if (currentLineIndex < anchorLineIndex)
        {
            viewModel.SelectionStart = currentLineStart;
            viewModel.SelectionEnd = anchorLineEnd;
            viewModel.CursorPosition = currentLineStart;
        }
        else
        {
            viewModel.SelectionStart = anchorLineStart;
            viewModel.SelectionEnd = currentLineEnd;
            viewModel.CursorPosition = currentLineEnd + 1;
        }

        var cursorPoint = GetPointFromPosition(viewModel.CursorPosition);
        HandleAutoScrollDuringSelection(cursorPoint, true);
    }


    private void HandleDoubleClickDrag(TextEditorViewModel viewModel, long position)
    {
        var (currentWordStart, currentWordEnd) = FindWordOrSymbolBoundaries(viewModel, position);
        var (anchorWordStart, anchorWordEnd) = FindWordOrSymbolBoundaries(viewModel, _selectionAnchor);

        if (position < _selectionAnchor)
        {
            viewModel.SelectionStart = Math.Min(currentWordStart, anchorWordStart);
            viewModel.SelectionEnd = Math.Max(anchorWordEnd, _selectionAnchor);
            viewModel.CursorPosition = currentWordStart;
        }
        else
        {
            viewModel.SelectionStart = Math.Min(anchorWordStart, _selectionAnchor);
            viewModel.SelectionEnd = Math.Max(currentWordEnd, anchorWordEnd);
            viewModel.CursorPosition = currentWordEnd;
        }

        HandleAutoScrollDuringSelection(GetPointFromPosition(viewModel.CursorPosition));
    }


    private void HandleNormalDrag(TextEditorViewModel viewModel, long position)
    {
        if (position < _selectionAnchor)
        {
            viewModel.SelectionStart = position;
            viewModel.SelectionEnd = _selectionAnchor;
        }
        else
        {
            viewModel.SelectionStart = _selectionAnchor;
            viewModel.SelectionEnd = position;
        }

        viewModel.CursorPosition = position;
        HandleAutoScrollDuringSelection(GetPointFromPosition(viewModel.CursorPosition));
    }
    
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_scrollableViewModel != null)
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;

            viewModel.IsSelecting = false;
            _scrollableViewModel.DisableHorizontalScrollToCursor = false;
            _scrollableViewModel.DisableVerticalScrollToCursor = false;
            _scrollTimer.Stop();
            _currentScrollSpeed = ScrollSpeed;

            // Ensure the selection is finalized
            UpdateSelection(viewModel);
        }

        _isDoubleClickDrag = false;
        _isTripleClickDrag = false;

        e.Handled = true;
    }

    private void HandleAutoScrollDuringSelection(Point cursorPoint, bool isTripleClickDrag = false)
    {
        if (_scrollableViewModel.TextEditorViewModel.ShouldScrollToCursor)
        {
            var horizontalThreshold = isTripleClickDrag ? HorizontalScrollThreshold * 1.5 : HorizontalScrollThreshold;
            var verticalThreshold = isTripleClickDrag ? VerticalScrollThreshold * 1.5 : VerticalScrollThreshold;

            CheckAndScrollHorizontally(cursorPoint.X, horizontalThreshold);
            CheckAndScrollVertically(cursorPoint.Y, verticalThreshold);
        }
    }

    private void UpdateSelection(TextEditorViewModel viewModel)
    {
        _lastKnownSelection = (viewModel.SelectionStart, viewModel.SelectionEnd);
        viewModel.SelectionStart = Math.Min(_selectionAnchor, viewModel.CursorPosition);
        viewModel.SelectionEnd = Math.Max(_selectionAnchor, viewModel.CursorPosition);
    }

    private void EnsureCursorVisible()
    {
        if (_scrollableViewModel?.TextEditorViewModel == null ||
            !_scrollableViewModel.TextEditorViewModel.ShouldScrollToCursor) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var cursorLine = GetLineIndexFromPosition(viewModel.CursorPosition);

        if (cursorLine < 0 || cursorLine >= viewModel.TextBuffer.LineStarts.Count)
            return;

        var cursorColumn = viewModel.CursorPosition - viewModel.TextBuffer.LineStarts[(int)cursorLine];

        if (!_scrollableViewModel.DisableVerticalScrollToCursor) AdjustVerticalScroll(cursorLine);

        if (!_scrollableViewModel.DisableHorizontalScrollToCursor || viewModel.ShouldScrollToCursor)
            AdjustHorizontalScroll(cursorColumn);

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
        if (_scrollableViewModel == null) return;

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

        // Check if the lineIndex is beyond the last line
        if (lineIndex >= GetLineCount()) return _scrollableViewModel.TextEditorViewModel.TextBuffer.Length;

        var column = (long)(point.X / _scrollableViewModel.TextEditorViewModel.CharWidth);

        lineIndex = Math.Max(0, Math.Min(lineIndex, GetLineCount() - 1));
        var lineStart = _scrollableViewModel.TextEditorViewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineLength = GetVisualLineLength(_scrollableViewModel.TextEditorViewModel, lineIndex);

        // If the click is beyond the end of the line text, set the column to the line length
        column = Math.Max(0, Math.Min(column, lineLength));

        return lineStart + column;
    }

    private void HandleTextInsertion(long position, string text)
    {
        if (_scrollableViewModel == null) throw new InvalidOperationException("_scrollableViewModel cannot be null.");

        var lineIndex = GetLineIndex(_scrollableViewModel.TextEditorViewModel, position);
        OnTextChanged(lineIndex);

        if (text.Contains('\n'))
            for (var i = lineIndex + 1; i < GetLineCount(); i++)
                OnTextChanged(i);
    }

    private void HandleTextDeletion(long position, long length)
    {
        if (_scrollableViewModel == null) throw new InvalidOperationException("_scrollableViewModel cannot be null.");

        var startLineIndex = GetLineIndex(_scrollableViewModel.TextEditorViewModel, position);
        var endLineIndex = GetLineIndex(_scrollableViewModel.TextEditorViewModel, position + length);

        for (var i = startLineIndex; i <= endLineIndex; i++) OnTextChanged(i);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (e.Text == "\t")
        {
            e.Handled = true;
            return;
        }

        base.OnTextInput(e);

        if (_scrollableViewModel != null && !string.IsNullOrEmpty(e.Text))
        {
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var insertPosition = viewModel.CursorPosition;

            // Use _lastKnownSelection if ViewModel's selection is cleared
            var selectionStart = viewModel.SelectionStart != viewModel.SelectionEnd
                ? viewModel.SelectionStart
                : _lastKnownSelection.Start;
            var selectionEnd = viewModel.SelectionStart != viewModel.SelectionEnd
                ? viewModel.SelectionEnd
                : _lastKnownSelection.End;

            if (selectionStart != selectionEnd)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var end = Math.Max(selectionStart, selectionEnd);
                var length = end - start;

                viewModel.DeleteText(start, length);
                HandleTextDeletion(start, length);
                insertPosition = start;
            }

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

            // Prevent default Tab behavior
            if (e.Key == Key.Tab)
            {
                HandleTab(viewModel, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                return;
            }

            HandleKeyDown(e, viewModel);

            var insertPosition = viewModel.CursorPosition;
            HandleTextInsertion(insertPosition, e.ToString());
            InvalidateVisual();
        }
    }

    private async void HandleTab(TextEditorViewModel viewModel, bool isShiftTab)
    {
        if (viewModel.SelectionStart != viewModel.SelectionEnd)
            await HandleTabForSelectionAsync(viewModel, isShiftTab);
        else if (isShiftTab)
            HandleShiftTabAtCursor(viewModel);
        else
            InsertTabAtCursor(viewModel);

        // Defer UI update to batch changes
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
    }

    private void HandleShiftTabAtCursor(TextEditorViewModel viewModel)
    {
        var tabString = UseTabCharacter ? "\t" : new string(' ', TabSize);
        var lineIndex = GetLineIndex(viewModel, viewModel.CursorPosition);
        var lineStart = viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
        var lineText = viewModel.TextBuffer.GetLineText(lineIndex);
        var cursorColumn = (int)(viewModel.CursorPosition - lineStart);

        // Check if there are tabs/spaces before the cursor position
        var deleteLength = 0;
        for (var i = cursorColumn - tabString.Length; i >= 0; i--)
            if (i >= 0 && i + tabString.Length <= lineText.Length &&
                lineText.Substring(i, tabString.Length) == tabString)
            {
                deleteLength = tabString.Length;
                viewModel.DeleteText(lineStart + i, deleteLength);
                HandleTextDeletion(lineStart + i, deleteLength);
                UpdateLineCache(lineIndex);

                viewModel.CursorPosition = lineStart + i;
                break;
            }

        // If no tabs/spaces were deleted before the cursor, unindent the line
        if (deleteLength == 0 && lineText.StartsWith(tabString))
        {
            viewModel.DeleteText(lineStart, tabString.Length);
            HandleTextDeletion(lineStart, tabString.Length);
            UpdateLineCache(lineIndex);

            viewModel.CursorPosition = Math.Max(viewModel.CursorPosition - tabString.Length, lineStart);
        }
    }

    private async Task HandleTabForSelectionAsync(TextEditorViewModel viewModel, bool isShiftTab)
    {
        if (_isTabOperationInProgress)
            return;

        lock (_tabOperationLock)
        {
            if (_isTabOperationInProgress)
                return;
            _isTabOperationInProgress = true;
        }

        try
        {
            var startLine = GetLineIndex(viewModel, Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd));
            var endLine = GetLineIndex(viewModel, Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd));

            var tabString = UseTabCharacter ? "\t" : new string(' ', TabSize);

            // Get modifications
            var entireTextSelected = IsEntireTextSelected(viewModel);
            var modifications = await Task.Run(() =>
                PrepareModifications(viewModel, (int)startLine, (int)endLine, tabString, isShiftTab));

            // Apply modifications directly on the UI thread
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ApplyModificationsAsync(viewModel, modifications);
                
                // Update selection based on modifications
                if (!entireTextSelected)
                    UpdateSelectionAfterTabbing(viewModel, startLine, endLine, isShiftTab, modifications);
                else if (isShiftTab)
                {
                    // Ensure selection end is within the new buffer length after shift tab
                    viewModel.SelectionEnd = Math.Min(viewModel.SelectionEnd, viewModel.TextBuffer.Length);
                    viewModel.CursorPosition = viewModel.SelectionEnd;
                }
                else
                {
                    viewModel.SelectionEnd = viewModel.TextBuffer.Length;
                    viewModel.CursorPosition = viewModel.SelectionEnd;
                }
                viewModel.NotifyGutterOfLineChange();
            });
        }
        finally
        {
            lock (_tabOperationLock)
            {
                _isTabOperationInProgress = false;
            }
        }
    }

    private bool IsEntireTextSelected(TextEditorViewModel viewModel)
    {
        return viewModel.SelectionStart == 0 && viewModel.SelectionEnd == viewModel.TextBuffer.Length;
    }

    private List<(long position, int deleteLength, string insertText)> PrepareModifications(
        TextEditorViewModel viewModel, int startLine, int endLine, string tabString, bool isShiftTab)
    {
        var modifications = new List<(long position, int deleteLength, string insertText)>();

        for (var lineIndex = startLine; lineIndex <= endLine; lineIndex++)
        {
            if (lineIndex < 0 || lineIndex >= viewModel.TextBuffer.LineCount)
            {
                Console.WriteLine(
                    $"Invalid lineIndex: {lineIndex}, LineStarts.Count: {viewModel.TextBuffer.LineStarts.Count}");
                continue;
            }

            var lineStart = viewModel.TextBuffer.GetLineStartPosition(lineIndex);
            var lineText = viewModel.TextBuffer.GetLineText(lineIndex);

            if (isShiftTab)
            {
                var positionToDelete = lineText.StartsWith(tabString) ? 0 : -1;
                if (positionToDelete >= 0)
                    modifications.Add((lineStart + positionToDelete, tabString.Length, string.Empty));
            }
            else
            {
                modifications.Add((lineStart, 0, tabString));
            }
        }

        return modifications;
    }

    private async Task<int> ApplyModificationsAsync(TextEditorViewModel viewModel,
        List<(long position, int deleteLength, string insertText)> modifications)
    {
        var actualTabLength = 0;
            long offset = 0;
            var sb = new StringBuilder(viewModel.TextBuffer.Text);

            foreach (var (position, originalDeleteLength, insertText) in modifications)
            {
                var deleteLength = originalDeleteLength;

                if (position + offset < 0 || position + offset >= sb.Length)
                {
                    Console.WriteLine($"Position {position + offset} is out of bounds. Skipping modification.");
                    continue;
                }

                if (deleteLength > 0)
                {
                    if (position + offset + deleteLength > sb.Length)
                    {
                        Console.WriteLine(
                            $"Delete length {deleteLength} from position {position + offset} exceeds string bounds. Adjusting length.");
                        deleteLength = sb.Length - (int)(position + offset);
                    }

                    sb.Remove((int)(position + offset), deleteLength);
                    offset -= deleteLength;
                    actualTabLength = deleteLength;
                }

                if (!string.IsNullOrEmpty(insertText))
                {
                    sb.Insert((int)(position + offset), insertText);
                    offset += insertText.Length;
                    actualTabLength = insertText.Length;
                }
            }

            // Final update of text buffer and line starts
            viewModel.TextBuffer.SetText(sb.ToString());

        return actualTabLength;
    }

    private void InsertTabAtCursor(TextEditorViewModel viewModel)
    {
        var tabString = UseTabCharacter ? "\t" : new string(' ', TabSize);
        viewModel.InsertText(viewModel.CursorPosition, tabString);
        HandleTextInsertion(viewModel.CursorPosition, tabString);
        viewModel.ClearSelection();
    }

    private void UpdateSelectionAfterTabbing(TextEditorViewModel viewModel, long startLine, long endLine,
        bool isShiftTab, List<(long position, int deleteLength, string insertText)> modifications)
    {
        if (isShiftTab)
        {
            var anyTabsRemoved = modifications.Any(m => m.deleteLength > 0);

            if (anyTabsRemoved)
            {
                var lineStartPos = viewModel.TextBuffer.GetLineStartPosition((int)startLine);
                var totalShift = modifications.Sum(m => m.deleteLength);
                viewModel.SelectionStart = Math.Max(viewModel.SelectionStart - totalShift, lineStartPos);

                var selectionEndOffset = 0;
                var lastLineIndex = -1.0;
                for (var lineIndex = startLine; lineIndex <= endLine; lineIndex++)
                {
                    var modificationsOnLine = modifications
                        .Where(m => GetLineIndex(viewModel, m.position) == lineIndex)
                        .Sum(m => m.deleteLength);
                    selectionEndOffset += modificationsOnLine;

                    lastLineIndex = lineIndex;
                }

                if (lastLineIndex < 0 || lastLineIndex >= viewModel.TextBuffer.LineCount)
                {
                    Console.WriteLine(
                        $"Invalid lastLineIndex: {lastLineIndex}, LineCount: {viewModel.TextBuffer.LineCount}");
                    lastLineIndex = viewModel.TextBuffer.LineCount - 1;
                }

                var lastLineEndPos = viewModel.TextBuffer.GetLineEndPosition((int)lastLineIndex);
                viewModel.SelectionEnd = Math.Min(viewModel.SelectionEnd - selectionEndOffset, lastLineEndPos);
                viewModel.SelectionEnd = Math.Min(viewModel.SelectionEnd, viewModel.TextBuffer.Length);

                // Adjust cursor position based on deletions
                var cursorShift = modifications.Where(m => m.position <= viewModel.CursorPosition)
                    .Sum(m => m.deleteLength);
                viewModel.CursorPosition = Math.Max(viewModel.CursorPosition - cursorShift, lineStartPos);
            }
        }
        else
        {
            var totalShift = modifications.Sum(m => m.insertText.Length);
            viewModel.SelectionStart = Math.Min(viewModel.SelectionStart, viewModel.TextBuffer.Length + 1);
            viewModel.SelectionEnd = Math.Min(viewModel.SelectionEnd + totalShift, viewModel.TextBuffer.Length);

            // Adjust cursor position based on insertions
            var cursorShift = modifications.Where(m => m.position <= viewModel.CursorPosition)
                .Sum(m => m.insertText.Length);
            viewModel.SelectionEnd = Math.Min(viewModel.SelectionEnd, viewModel.TextBuffer.Length);
            viewModel.CursorPosition += cursorShift;
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

        if (shiftFlag && _selectionAnchor == -1)
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
            case Key.Tab:
            {
                e.Handled = true;
                return;
            }
        }

        if (shiftFlag)
        {
            UpdateSelection(viewModel);
        }
        else if (!IsNonPrintableKey(e.Key))
        {
            viewModel.ClearSelection();
            _selectionAnchor = -1;
        }

        viewModel.CursorPosition = Math.Min(Math.Max(viewModel.CursorPosition, 0), viewModel.TextBuffer.Length);
        InvalidateVisual();
    }

    private bool IsNonPrintableKey(Key key)
    {
        return key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End
            or Key.PageUp or Key.PageDown or Key.Insert or Key.Delete or Key.Back
            or Key.Tab or Key.Enter or Key.Escape;
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
            if (lineIndex > 0) viewModel.CursorPosition = viewModel.TextBuffer.GetLineEndPosition((int)(lineIndex - 1));
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
            var start = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
            var end = Math.Max(viewModel.SelectionStart, viewModel.SelectionEnd);
            var length = end - start;

            if (start >= 0 && end <= viewModel.TextBuffer.Length)
            {
                viewModel.DeleteText(start, length);
                viewModel.CursorPosition = start;
            }
            else
            {
                Console.WriteLine($"Start {start} or end {end} is out of bounds for deletion.");
                return;
            }
        }

        var insertPosition = viewModel.CursorPosition;
        if (insertPosition < 0 || insertPosition > viewModel.TextBuffer.Length)
        {
            Console.WriteLine($"Insert position {insertPosition} is out of bounds.");
            return;
        }

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
        var lineText = viewModel.TextBuffer.GetLineText((int)lineIndex);
        return lineText.TrimEnd('\n', '\r').Length; // Exclude line ending characters
    }

    private long GetLineIndex(TextEditorViewModel viewModel, long position)
    {
        if (viewModel?.TextBuffer?.Rope == null)
            throw new ArgumentNullException(nameof(viewModel), "TextEditorViewModel or its properties cannot be null.");
        return viewModel.TextBuffer.Rope.GetLineIndexFromPosition((int)position);
    }

    public override void Render(DrawingContext context)
    {
        if (_scrollableViewModel == null) return;

        context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));

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

        var totalWidth = Math.Max(viewModel.WindowWidth, viewableAreaWidth + _scrollableViewModel!.HorizontalOffset);

        if (cursorLine < selectionStartLine || cursorLine > selectionEndLine)
        {
            var rect = new Rect(0, y, totalWidth, LineHeight);
            context.FillRectangle(LineHighlightBrush, rect);
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

            if (xStart > 0)
            {
                var beforeSelectionRect = new Rect(0, y, xStart, LineHeight);
                context.FillRectangle(LineHighlightBrush, beforeSelectionRect);
            }

            var afterSelectionRect = new Rect(xEnd, y, totalWidth - xEnd, LineHeight);
            context.FillRectangle(LineHighlightBrush, afterSelectionRect);
        }
    }

    private void RenderVisibleLines(DrawingContext context, ScrollableTextEditorViewModel scrollableViewModel,
        long firstVisibleLine, long lastVisibleLine, double viewableAreaWidth)
    {
        const int startIndexBuffer = 5;
        var yOffset = firstVisibleLine * LineHeight;

        for (var i = firstVisibleLine; i < lastVisibleLine; i++)
        {
            var lineText = scrollableViewModel.TextEditorViewModel.TextBuffer.GetLineText((int)i);
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
                Foreground);

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
                selectionWidth += 2;
            }

            selectionWidth = Math.Max(selectionWidth, viewModel.CharWidth);

            var selectionRect = new Rect(xStart, y, selectionWidth, LineHeight);
            context.FillRectangle(SelectionBrush, selectionRect);
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
                new Pen(CursorBrush),
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

            viewModel.ShouldScrollToCursor = false;
            viewModel.CursorPosition = viewModel.TextBuffer.Length;
            viewModel.ShouldScrollToCursor = true;

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

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var insertPosition = viewModel.CursorPosition;

            // Use StringBuilder for efficient text manipulation
            var sb = new StringBuilder(viewModel.TextBuffer.Text);

            // Handle selection deletion in a single operation
            if (viewModel.SelectionStart != -1 && viewModel.SelectionEnd != -1)
            {
                var start = Math.Min(viewModel.SelectionStart, viewModel.SelectionEnd);
                var length = Math.Abs(viewModel.SelectionEnd - viewModel.SelectionStart);
                sb.Remove((int)start, (int)length);
                insertPosition = start;
            }

            // Perform text insertion as a single operation
            sb.Insert((int)insertPosition, text);

            // Set the modified text back to the text buffer
            viewModel.TextBuffer.SetText(sb.ToString());

            // Update line starts only once
            viewModel.UpdateLineStarts();

            viewModel.CursorPosition = insertPosition + text.Length;
            UpdateDesiredColumn(viewModel);

            viewModel.ClearSelection();
            _lastKnownSelection = (viewModel.CursorPosition, viewModel.CursorPosition);

            // Defer UI updates
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            Dispatcher.UIThread.Post(UpdateHorizontalScrollPosition, DispatcherPriority.Background);
            Dispatcher.UIThread.Post(EnsureCursorVisible, DispatcherPriority.Background);
            Dispatcher.UIThread.Post(_scrollableViewModel.TextEditorViewModel.UpdateGutterWidth,
                DispatcherPriority.Background);
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