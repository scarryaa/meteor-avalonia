using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.ViewModels;
using ReactiveUI;

namespace meteor.Views;

public partial class Gutter : UserControl
{
    private bool _isDragging;
    private long _dragStartLine;
    private const double ScrollThreshold = 10;
    private const double ScrollSpeed = 1;
    private readonly Dictionary<long, FormattedText> formattedTextCache = new();
    private (long start, long end) _lastKnownSelection = (-1, -1);

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<Gutter, double>(nameof(LineHeight), 20);

    public static readonly StyledProperty<IBrush> BackgroundBrushProperty =
        AvaloniaProperty.Register<Gutter, IBrush>(nameof(BackgroundBrush), Brushes.White);

    public static readonly StyledProperty<IBrush> ForegroundBrushProperty =
        AvaloniaProperty.Register<Gutter, IBrush>(nameof(ForegroundBrush));

    public static readonly StyledProperty<IBrush> LineHighlightBrushProperty =
        AvaloniaProperty.Register<Gutter, IBrush>(nameof(LineHighlightBrush));

    public static readonly StyledProperty<IBrush> SelectedBrushProperty =
        AvaloniaProperty.Register<Gutter, IBrush>(nameof(SelectedBrush),
            new SolidColorBrush(Color.FromArgb(100, 139, 205, 192)));

    public new static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<Gutter, FontFamily>(
            nameof(FontFamily),
            new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono"));

    public new static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<Gutter, double>(nameof(FontSize), 13);

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

    public IBrush ForegroundBrush
    {
        get => GetValue(ForegroundProperty);
        set
        {
            SetValue(ForegroundProperty, value);
            InvalidateVisual();
        }
    }

    public IBrush BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set
        {
            SetValue(BackgroundBrushProperty, value);
            InvalidateVisual();
        }
    }

    public IBrush LineHighlightBrush
    {
        get => GetValue(LineHighlightBrushProperty);
        set => SetValue(LineHighlightBrushProperty, value);
    }

    public IBrush SelectedBrush
    {
        get => GetValue(SelectedBrushProperty);
        set => SetValue(SelectedBrushProperty, value);
    }

    public Gutter()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        this.GetObservable(ForegroundBrushProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(SelectedBrushProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(FontFamilyProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(FontSizeProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(BackgroundBrushProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(LineHighlightBrushProperty).Subscribe(_ => { InvalidateVisual(); });
        
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerEnteredEvent, OnPointerEntered, RoutingStrategies.Tunnel);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is GutterViewModel oldViewModel) oldViewModel.InvalidateRequired -= OnInvalidateRequired;

        if (DataContext is GutterViewModel newViewModel)
        {
            newViewModel.InvalidateRequired += OnInvalidateRequired;
            Bind(LineHeightProperty, newViewModel.WhenAnyValue(vm => vm.LineHeight));
            Bind(FontFamilyProperty, newViewModel.WhenAnyValue(vm => vm.FontFamily));
            Bind(FontSizeProperty, newViewModel.WhenAnyValue(vm => vm.FontSize));

            UpdateGutterWidth(newViewModel);

            newViewModel.TextEditorViewModel.WhenAnyValue(lvm => lvm.LineCount)
                .Subscribe(_ => UpdateGutterWidth(newViewModel));
            newViewModel.WhenAnyValue(vm => vm.FontSize).Subscribe(_ => UpdateGutterWidth(newViewModel));
        }
    }

    private void OnInvalidateRequired(object? sender, EventArgs e)
    {
        InvalidateVisual();
        InvalidateMeasure();
        InvalidateArrange();
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is GutterViewModel viewModel)
        {
            var delta = e.Delta.Y * 3 * viewModel.LineHeight;
            var newOffset = viewModel.VerticalOffset - delta;
            var maxOffset = Math.Max(0, (double)viewModel.TextEditorViewModel.TextBuffer.LineCount * viewModel.LineHeight - Bounds.Height + 6);

            viewModel.VerticalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));
            viewModel.LineCountViewModel.VerticalOffset = viewModel.VerticalOffset;
            e.Handled = true;
        }
    }

    private long GetLineNumberFromY(double y)
    {
        if (DataContext is GutterViewModel viewModel)
        {
            var lineNumber = (long)Math.Floor((y + viewModel.VerticalOffset) / LineHeight);
            return Math.Max(0, Math.Min(lineNumber, viewModel.TextEditorViewModel.TextBuffer.LineCount - 1));
        }

        return 0;
    }

    private void HandleAutoScroll(GutterViewModel viewModel, double y)
    {
        if (y < ScrollThreshold)
        {
            viewModel.VerticalOffset = Math.Max(0, viewModel.VerticalOffset - ScrollSpeed * LineHeight);
        }
        else if (y > Bounds.Height - ScrollThreshold)
        {
            var maxOffset = Math.Max(0, (double)viewModel.TextEditorViewModel.TextBuffer.LineCount * LineHeight - Bounds.Height);
            viewModel.VerticalOffset = Math.Min(maxOffset, viewModel.VerticalOffset + ScrollSpeed * LineHeight);
        }
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (DataContext is GutterViewModel viewModel)
        {
            var textEditorViewModel = viewModel.TextEditorViewModel;
            if (textEditorViewModel.IsSelecting)
            {
                _isDragging = true;
                _dragStartLine = GetLineIndexFromPosition(textEditorViewModel.SelectionStart);
                e.Handled = true;
            }
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is GutterViewModel viewModel)
        {
            if (viewModel.ScrollableTextEditorViewModel is ScrollableTextEditorViewModel scrollableViewModel)
            {
                scrollableViewModel.DisableVerticalScrollToCursor = true;
                scrollableViewModel.DisableHorizontalScrollToCursor = true;
            }
            
            _isDragging = true;
            _dragStartLine = GetLineNumberFromY(e.GetPosition(this).Y);

            UpdateSelection(viewModel, _dragStartLine, _dragStartLine);
            
            var lineStartPosition = viewModel.TextEditorViewModel.TextBuffer.GetLineStartPosition((int)_dragStartLine);
            viewModel.TextEditorViewModel.CursorPosition = lineStartPosition;
            viewModel.TextEditorViewModel.OnInvalidateRequired();
            viewModel.TextEditorViewModel.ShouldScrollToCursor = false;

            e.Handled = true;
            InvalidateVisual();
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is GutterViewModel viewModel)
        {
            var position = e.GetPosition(this);
            var currentLine = GetLineNumberFromY(position.Y);

            if (_isDragging)
            {
                UpdateSelection(viewModel, _dragStartLine, currentLine);
                HandleAutoScroll(viewModel, position.Y);
            }
            else if (viewModel.TextEditorViewModel.IsSelecting)
            {
                // Continue selection from text editor
                _isDragging = true;
                _dragStartLine = GetLineIndexFromPosition(viewModel.TextEditorViewModel.SelectionStart);
                UpdateSelection(viewModel, _dragStartLine, currentLine);
                HandleAutoScroll(viewModel, position.Y);
            }

            e.Handled = true;
            InvalidateVisual();
        }
    }
    
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is GutterViewModel viewModel)
        {
            _isDragging = false;

            var textEditorViewModel = viewModel.TextEditorViewModel;

            if (viewModel.ScrollableTextEditorViewModel is ScrollableTextEditorViewModel scrollableViewModel)
            {
                scrollableViewModel.DisableHorizontalScrollToCursor = false;
                scrollableViewModel.DisableVerticalScrollToCursor = false;
            }

            // Delay re-enabling scroll to cursor
            textEditorViewModel.ShouldScrollToCursor = false;
            Dispatcher.UIThread.Post(() => textEditorViewModel.ShouldScrollToCursor = true,
                DispatcherPriority.Background);
            
            InvalidateVisual();
        }
    }
    
    private void UpdateSelection(GutterViewModel viewModel, long startLine, long endLine)
    {
        var textEditorViewModel = viewModel.TextEditorViewModel;
        var textBuffer = textEditorViewModel.TextBuffer;

        startLine = Math.Max(0, Math.Min(startLine, textBuffer.LineCount - 1));
        endLine = Math.Max(0, Math.Min(endLine, textBuffer.LineCount - 1));

        var selectionStartLine = Math.Min(startLine, endLine);
        var selectionEndLine = Math.Max(startLine, endLine);

        var selectionStart = textBuffer.GetLineStartPosition((int)selectionStartLine);
        var selectionEnd = textBuffer.GetLineEndPosition((int)selectionEndLine);

        // If the selection ends at the last line, include the last character
        if (selectionEndLine == textBuffer.LineCount - 1) selectionEnd++; // Adjust to include the last character of the last line

        // Ensure the selection includes the entire range from start to end lines
        textEditorViewModel.SelectionStart = selectionStart;
        textEditorViewModel.SelectionEnd = selectionEnd;

        // Set the cursor position based on the selection direction
        textEditorViewModel.ShouldScrollToCursor = false;
        textEditorViewModel.CursorPosition = startLine <= endLine ? selectionEnd : selectionStart;
        textEditorViewModel.ShouldScrollToCursor = true;

        textEditorViewModel.NotifySelectionChanged(textEditorViewModel.SelectionStart,
            textEditorViewModel.SelectionEnd);
        InvalidateVisual();
    }
    
    private void UpdateGutterWidth(GutterViewModel viewModel)
    {
        var maxLineNumber = viewModel.TextEditorViewModel.LineCount;
        var formattedTextMaxLine = new FormattedText(
            maxLineNumber.ToString(),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily),
            viewModel.FontSize,
            Brushes.Gray);

        var formattedText9999 = new FormattedText(
            "9999",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily),
            viewModel.FontSize,
            Brushes.Gray);

        Width = Math.Max(formattedTextMaxLine.Width, formattedText9999.Width) + 40;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));

        if (DataContext is not GutterViewModel viewModel) return;

        var firstVisibleLine = (int)Math.Max(0, Math.Floor(viewModel.VerticalOffset / LineHeight));
        var lastVisibleLine = (int)Math.Ceiling((viewModel.VerticalOffset + viewModel.ViewportHeight) / LineHeight);
        firstVisibleLine = Math.Max(0, firstVisibleLine);
        lastVisibleLine = Math.Min((int)viewModel.TextEditorViewModel.TextBuffer.LineCount - 1, lastVisibleLine);

        var selectionStart = viewModel.TextEditorViewModel.SelectionStart;
        var selectionEnd = viewModel.TextEditorViewModel.SelectionEnd;
        var cursorLine = GetLineIndexFromPosition(viewModel.TextEditorViewModel.CursorPosition);
        var textBuffer = viewModel.TextEditorViewModel.TextBuffer;

        if (selectionStart != _lastKnownSelection.start || selectionEnd != _lastKnownSelection.end)
        {
            formattedTextCache.Clear();
            _lastKnownSelection = (selectionStart, selectionEnd);
        }

        for (var i = firstVisibleLine; i <= lastVisibleLine; i++)
        {
            var lineNumber = i + 1;
            var yPosition = i * LineHeight - viewModel.VerticalOffset;

            var isSelected = false;
            if (i >= 0 && i < textBuffer.LineCount)
                try
                {
                    isSelected = textBuffer.IsLineSelected(i, selectionStart, selectionEnd);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Console.WriteLine(
                        $"Error in IsLineSelected: {ex.Message}. Line index: {i}, Selection: {selectionStart}-{selectionEnd}, Total lines: {textBuffer.LineCount}");
                }

            var isCurrentLine = i == cursorLine;

            var brush = (isSelected && _lastKnownSelection.start != _lastKnownSelection.end) || isCurrentLine
                ? SelectedBrush
                : ForegroundBrush;

            var formattedText = new FormattedText(
                lineNumber.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily),
                FontSize,
                brush);
            formattedTextCache[i] = formattedText;

            var verticalOffset = (LineHeight - formattedText.Height) / 2;
            yPosition += verticalOffset;

            if (i == cursorLine)
            {
                var highlightRect = new Rect(0, yPosition - verticalOffset, Bounds.Width, LineHeight);
                context.FillRectangle(LineHighlightBrush, highlightRect);
            }

            context.DrawText(formattedText, new Point(Bounds.Width - formattedText.Width - 20, yPosition));
        }
    }

    private int GetLineIndexFromPosition(long position)
    {
        if (DataContext is not GutterViewModel viewModel) return 0;

        var textBuffer = viewModel.TextEditorViewModel.TextBuffer;

        var lineIndex = textBuffer.GetLineIndexFromPosition((int)position);
        return (int)lineIndex;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (DataContext is GutterViewModel viewModel) viewModel.InvalidateRequired -= OnInvalidateRequired;
    }
}
