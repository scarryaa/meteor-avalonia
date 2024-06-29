using System;
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
            // Adjust the vertical offset based on the delta of the wheel event
            var delta = e.Delta.Y * 3 * viewModel.LineHeight;
            var newOffset = viewModel.VerticalOffset - delta;

            // Clamp the new offset between 0 and the maximum allowed offset
            var maxOffset = Math.Max(0, (double)viewModel.LineCount * viewModel.LineHeight - Bounds.Height + 6);
            viewModel.VerticalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));
            viewModel.LineCountViewModel.VerticalOffset = viewModel.VerticalOffset;

            // Prevent the event from bubbling up
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
            // Scroll up
            viewModel.VerticalOffset = Math.Max(0, viewModel.VerticalOffset - ScrollSpeed * LineHeight);
        }
        else if (y > Bounds.Height - ScrollThreshold)
        {
            // Scroll down
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

        // Ensure start and end lines are within bounds
        startLine = BigInteger.Max(BigInteger.Zero, BigInteger.Min(startLine, viewModel.LineCount - 1));
        endLine = BigInteger.Max(BigInteger.Zero, BigInteger.Min(endLine, viewModel.LineCount - 1));

        // Determine the actual start and end of the selection
        var selectionStartLine = BigInteger.Min(startLine, endLine);
        var selectionEndLine = BigInteger.Max(startLine, endLine);

        var selectionStart = rope.GetLineStartPosition((int)selectionStartLine);
        var selectionEnd = rope.GetLineEndPosition((int)selectionEndLine);

        // Update the selection in the TextEditorViewModel
        textEditorViewModel.SelectionStart = selectionStart;
        textEditorViewModel.SelectionEnd = selectionEnd;

        // Set the cursor position based on the drag direction
        if (startLine <= endLine)
            // Dragging downwards or no movement
            textEditorViewModel.CursorPosition = selectionEnd;
        else
            // Dragging upwards
            textEditorViewModel.CursorPosition = selectionStart;

        InvalidateVisual();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is GutterViewModel oldViewModel)
            oldViewModel.InvalidateRequired -= OnInvalidateRequired;

        if (DataContext is GutterViewModel newViewModel)
        {
            newViewModel.InvalidateRequired += OnInvalidateRequired;
            Bind(LineHeightProperty, newViewModel.WhenAnyValue(vm => vm.LineHeight));
            Bind(FontFamilyProperty, newViewModel.WhenAnyValue(vm => vm.FontFamily));
            Bind(FontSizeProperty, newViewModel.WhenAnyValue(vm => vm.FontSize));

            // Update the width of the gutter based on the max line number
            UpdateGutterWidth(newViewModel);
            newViewModel.LineCountViewModel.WhenAnyValue(lvm => lvm.MaxLineNumber)
                .Subscribe(_ => UpdateGutterWidth(newViewModel));
            newViewModel.WhenAnyValue(vm => vm.FontSize).Subscribe(_ => UpdateGutterWidth(newViewModel));
        }
    }

    private void UpdateGutterWidth(GutterViewModel viewModel)
    {
        // Calculate the width of the maximum line number
        var maxLineNumber = viewModel.LineCountViewModel.MaxLineNumber;
        var formattedTextMaxLine = new FormattedText(
            maxLineNumber.ToString(),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily),
            viewModel.FontSize,
            Brushes.Gray
        );

        // Calculate the width of '9999' for the minimum width enforcement
        var formattedText9999 = new FormattedText(
            "9999",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily),
            viewModel.FontSize,
            Brushes.Gray
        );

        // Determine the gutter width
        Width = Math.Max(formattedTextMaxLine.Width, formattedText9999.Width) + 10;
    }

    private void OnInvalidateRequired(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.FillRectangle(Brushes.White, new Rect(Bounds.Size));

        if (DataContext is GutterViewModel viewModel)
        {
            var firstVisibleLine = (BigInteger)Math.Max(0, Math.Floor(viewModel.VerticalOffset / LineHeight));
            var lastVisibleLine =
                (BigInteger)Math.Ceiling((viewModel.VerticalOffset + viewModel.ViewportHeight) / LineHeight);

            lastVisibleLine = BigInteger.Max(0, lastVisibleLine);
            firstVisibleLine = BigInteger.Max(0, firstVisibleLine);
            lastVisibleLine = BigInteger.Min(viewModel.LineCountViewModel.LineCount - 1, lastVisibleLine + 1);

            var selectionStart = viewModel.TextEditorViewModel.SelectionStart;
            var selectionEnd = viewModel.TextEditorViewModel.SelectionEnd;
            var cursorLine = GetLineIndexFromPosition(viewModel.TextEditorViewModel.CursorPosition);
            var rope = viewModel.TextEditorViewModel.Rope;

            for (var i = firstVisibleLine; i <= lastVisibleLine; i++)
            {
                var lineNumber = i + 1;
                var yPosition = (double)i * LineHeight - viewModel.VerticalOffset;

                // Check if this line is within the selection
                var lineStart = BigInteger.Zero;
                var lineEnd = BigInteger.Zero;

                // Ensure the line indices are within the valid range
                if (i < rope.LineCount)
                {
                    lineStart = rope.GetLineStartPosition((int)i);
                    lineEnd = rope.GetLineEndPosition((int)i);
                }

                var isSelected = (lineStart >= selectionStart && lineStart < selectionEnd) ||
                                 (lineEnd > selectionStart && lineEnd <= selectionEnd) ||
                                 (lineStart <= selectionStart && lineEnd >= selectionEnd);

                var formattedText = new FormattedText(
                    lineNumber.ToString(),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily),
                    FontSize,
                    isSelected ? Brushes.Black : new SolidColorBrush(Color.Parse("#bbbbbb"))
                );

                var verticalOffset = (LineHeight - formattedText.Height) / 2;
                yPosition += verticalOffset;

                // Highlight the background for the current line
                if (i == cursorLine)
                {
                    var highlightRect = new Rect(0, yPosition - verticalOffset, Bounds.Width, LineHeight);
                    context.FillRectangle(new SolidColorBrush(Color.Parse("#ededed")), highlightRect);
                }

                context.DrawText(formattedText, new Point(Bounds.Width - formattedText.Width - 5, yPosition));
            }
        }
    }

    private BigInteger GetLineIndexFromPosition(BigInteger position)
    {
        var rope = DataContext is GutterViewModel viewModel ? viewModel.TextEditorViewModel.Rope : null;
        if (rope == null) return BigInteger.Zero;

        var lineIndex = BigInteger.Zero;
        var accumulatedLength = BigInteger.Zero;

        while (lineIndex < rope.LineCount &&
               accumulatedLength + rope.GetLineLength((int)lineIndex) <= position)
        {
            accumulatedLength += rope.GetLineLength((int)lineIndex);
            lineIndex++;
        }

        // Ensure lineIndex does not exceed the line count
        lineIndex = BigInteger.Max(BigInteger.Zero, BigInteger.Min(lineIndex, rope.LineCount - 1));

        return lineIndex;
    }

    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (DataContext is GutterViewModel viewModel)
            viewModel.InvalidateRequired -= OnInvalidateRequired;
    }
}