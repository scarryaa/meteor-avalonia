using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using meteor.ViewModels;
using ReactiveUI;

namespace meteor.Views;

public partial class Gutter : UserControl
{
    private bool _isDragging;
    private BigInteger _dragStartLine;
    private const double ScrollThreshold = 10;
    private const double ScrollSpeed = 1;
    private readonly Dictionary<BigInteger, FormattedText> formattedTextCache = new();
    private (BigInteger start, BigInteger end) _lastKnownSelection = (-1, -1);
    private int _lastRenderedLine = -1;
    private double _lastRenderedOffset = -1;

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<Gutter, FontFamily>(nameof(FontFamily));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<Gutter, double>(nameof(FontSize));

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<Gutter, double>(nameof(LineHeight), 20);

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

    public Gutter()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is GutterViewModel viewModel)
        {
            var delta = e.Delta.Y * 3 * viewModel.LineHeight;
            var newOffset = viewModel.VerticalOffset - delta;
            var maxOffset = Math.Max(0, (double)viewModel.LineCount * viewModel.LineHeight - Bounds.Height + 6);

            viewModel.VerticalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));
            viewModel.LineCountViewModel.VerticalOffset = viewModel.VerticalOffset;
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && DataContext is GutterViewModel viewModel)
        {
            var position = e.GetPosition(this);
            var currentLine = GetLineNumberFromY(position.Y);
            UpdateSelection(viewModel, _dragStartLine, currentLine);
            HandleAutoScroll(viewModel, position.Y);
            e.Handled = true;
        }
    }

    private void HandleAutoScroll(GutterViewModel viewModel, double y)
    {
        if (y < ScrollThreshold)
        {
            viewModel.VerticalOffset = Math.Max(0, viewModel.VerticalOffset - ScrollSpeed * LineHeight);
        }
        else if (y > Bounds.Height - ScrollThreshold)
        {
            var maxOffset = Math.Max(0, (double)viewModel.LineCount * LineHeight - Bounds.Height);
            viewModel.VerticalOffset = Math.Min(maxOffset, viewModel.VerticalOffset + ScrollSpeed * LineHeight);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is GutterViewModel viewModel)
        {
            _isDragging = true;
            _dragStartLine = GetLineNumberFromY(e.GetPosition(this).Y);
            UpdateSelection(viewModel, _dragStartLine, _dragStartLine);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    private BigInteger GetLineNumberFromY(double y)
    {
        if (DataContext is GutterViewModel viewModel)
        {
            var lineNumber = (BigInteger)Math.Floor((y + viewModel.VerticalOffset) / LineHeight);
            return BigInteger.Max(BigInteger.Zero, BigInteger.Min(lineNumber, viewModel.LineCount - 1));
        }

        return BigInteger.Zero;
    }

    private void UpdateSelection(GutterViewModel viewModel, BigInteger startLine, BigInteger endLine)
    {
        var textEditorViewModel = viewModel.TextEditorViewModel;
        var rope = textEditorViewModel.Rope;

        startLine = BigInteger.Max(BigInteger.Zero, BigInteger.Min(startLine, viewModel.LineCount - 1));
        endLine = BigInteger.Max(BigInteger.Zero, BigInteger.Min(endLine, viewModel.LineCount - 1));

        var selectionStartLine = BigInteger.Min(startLine, endLine);
        var selectionEndLine = BigInteger.Max(startLine, endLine);

        var selectionStart = rope.GetLineStartPosition((int)selectionStartLine);
        var selectionEnd = rope.GetLineEndPosition((int)selectionEndLine);

        textEditorViewModel.SelectionStart = selectionStart;
        textEditorViewModel.SelectionEnd = selectionEnd;

        // textEditorViewModel.CursorPosition = startLine <= endLine ? selectionEnd : selectionStart;

        InvalidateVisual();
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

            newViewModel.LineCountViewModel.WhenAnyValue(lvm => lvm.MaxLineNumber)
                .Subscribe(_ => UpdateGutterWidth(newViewModel));
            newViewModel.WhenAnyValue(vm => vm.FontSize).Subscribe(_ => UpdateGutterWidth(newViewModel));
        }
    }

    private void UpdateGutterWidth(GutterViewModel viewModel)
    {
        var maxLineNumber = viewModel.LineCountViewModel.MaxLineNumber;
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

        Width = Math.Max(formattedTextMaxLine.Width, formattedText9999.Width) + 10;
    }

    private void OnInvalidateRequired(object? sender, EventArgs e)
    {
        InvalidateVisual();
        InvalidateMeasure();
        InvalidateArrange();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.FillRectangle(Brushes.White, new Rect(Bounds.Size));

        if (DataContext is not GutterViewModel viewModel) return;

        var firstVisibleLine = (int)Math.Max(0, Math.Floor(viewModel.VerticalOffset / LineHeight));
        var lastVisibleLine = (int)Math.Ceiling((viewModel.VerticalOffset + viewModel.ViewportHeight) / LineHeight);
        firstVisibleLine = Math.Max(0, firstVisibleLine);
        lastVisibleLine = Math.Min((int)viewModel.LineCountViewModel.LineCount - 1, lastVisibleLine);

        var selectionStart = viewModel.TextEditorViewModel.SelectionStart;
        var selectionEnd = viewModel.TextEditorViewModel.SelectionEnd;
        var cursorLine = GetLineIndexFromPosition(viewModel.TextEditorViewModel.CursorPosition);
        var rope = viewModel.TextEditorViewModel.Rope;

        // Invalidate the cache if the selection has changed
        if (selectionStart != _lastKnownSelection.start || selectionEnd != _lastKnownSelection.end)
        {
            formattedTextCache.Clear();
            _lastKnownSelection = (selectionStart, selectionEnd);
        }

        for (var i = firstVisibleLine; i <= lastVisibleLine; i++)
        {
            var lineNumber = i + 1;
            var yPosition = i * LineHeight - viewModel.VerticalOffset;

            var formattedText = formattedTextCache.GetValueOrDefault(i);
            var isSelected = rope.IsLineSelected(i, selectionStart, selectionEnd);
            var isCurrentLine = i == cursorLine;

            var brush = (isSelected && _lastKnownSelection.start != _lastKnownSelection.end) || isCurrentLine
                ? new SolidColorBrush(Brushes.Black.Color)
                : new SolidColorBrush(Color.Parse("#bbbbbb"));

            formattedText = new FormattedText(
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
                context.FillRectangle(new SolidColorBrush(Color.Parse("#ededed")), highlightRect);
            }

            context.DrawText(formattedText, new Point(Bounds.Width - formattedText.Width - 5, yPosition));
        }
    }

    private int GetLineIndexFromPosition(BigInteger position)
    {
        if (DataContext is not GutterViewModel viewModel) return 0;

        var rope = viewModel.TextEditorViewModel.Rope;
        var lineIndex = rope.GetLineIndexFromPosition((int)position);
        
        return lineIndex;
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (DataContext is GutterViewModel viewModel) viewModel.InvalidateRequired -= OnInvalidateRequired;
    }
}
