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
            var delta = e.Delta.Y * viewModel.LineHeight;
            var newOffset = viewModel.VerticalOffset - delta;

            // Clamp the new offset between 0 and the maximum allowed offset
            var maxOffset = Math.Max(0, (double)viewModel.LineCount * viewModel.LineHeight - Bounds.Height + 5);
            viewModel.VerticalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));

            // Prevent the event from bubbling up
            e.Handled = true;
        }
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
        Width = Math.Max(formattedTextMaxLine.Width, formattedText9999.Width) + 10; // Add some padding
    }

    private void OnInvalidateRequired(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (DataContext is GutterViewModel viewModel)
        {
            var verticalBufferLines = 50;

            // Calculate the first visible line
            var firstVisibleLine =
                (BigInteger)Math.Max(0, Math.Floor(viewModel.LineCountViewModel.VerticalOffset / LineHeight));

            // Calculate the last visible line
            var lastVisibleLine =
                (BigInteger)Math.Ceiling((viewModel.LineCountViewModel.VerticalOffset + viewModel.ViewportHeight) /
                                         LineHeight);

            // Ensure lastVisibleLine is not less than 0
            lastVisibleLine = BigInteger.Max(0, lastVisibleLine);

            // Extend the range by buffer lines
            firstVisibleLine = BigInteger.Max(0, firstVisibleLine - verticalBufferLines);
            lastVisibleLine = BigInteger.Min(viewModel.LineCountViewModel.LineCount - 1,
                lastVisibleLine + verticalBufferLines + 1);

            var hasDrawnLine = false;
            for (var i = firstVisibleLine; i <= lastVisibleLine; i++)
            {
                var lineNumber = i + 1;
                var yPosition = (double)i * LineHeight - viewModel.VerticalOffset;

                var formattedText = new FormattedText(
                    lineNumber.ToString(),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily),
                    FontSize,
                    Brushes.Gray
                );

                // Adjust yPosition to center the text vertically within the line
                var verticalOffset = (LineHeight - formattedText.Height) / 2;
                yPosition += verticalOffset;

                // Ensure that at least one line is rendered
                if (yPosition + LineHeight >= 0 && yPosition <= Bounds.Height)
                {
                    context.DrawText(formattedText, new Point(Bounds.Width - formattedText.Width - 5, yPosition));
                    hasDrawnLine = true;
                }
                else if (!hasDrawnLine)
                {
                    context.DrawText(formattedText, new Point(Bounds.Width - formattedText.Width - 5, yPosition));
                    hasDrawnLine = true;
                }
            }
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (DataContext is GutterViewModel viewModel)
            viewModel.InvalidateRequired -= OnInvalidateRequired;
    }
}