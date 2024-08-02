using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;
using System.Globalization;

namespace meteor.UI.Features.SearchView.Controls
{
    public class FilterButton : Control
    {
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<FilterButton, string>(nameof(Text));

        public static readonly StyledProperty<string> TooltipProperty =
            AvaloniaProperty.Register<FilterButton, string>(nameof(Tooltip));

        public static readonly StyledProperty<bool> IsActiveProperty =
            AvaloniaProperty.Register<FilterButton, bool>(nameof(IsActive), defaultValue: false);

        private bool _isHovered;

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Tooltip
        {
            get => GetValue(TooltipProperty);
            set => SetValue(TooltipProperty, value);
        }

        public bool IsActive
        {
            get => GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public event EventHandler<bool> FilterToggled;

        public FilterButton()
        {
            Width = 24;
            Height = 24;
            PointerEntered += OnPointerEntered;
            PointerExited += OnPointerExited;
        }

        public FilterButton(string text, string tooltip) : this()
        {
            Text = text;
            Tooltip = tooltip;
        }

        private void OnPointerEntered(object sender, PointerEventArgs e)
        {
            _isHovered = true;
            InvalidateVisual();
            ToolTip.SetTip(this, Tooltip);
        }

        private void OnPointerExited(object sender, PointerEventArgs e)
        {
            _isHovered = false;
            InvalidateVisual();
            ToolTip.SetTip(this, null);
        }

        public override void Render(DrawingContext context)
        {
            var backgroundBrush = IsActive ? Brushes.LightGray : (_isHovered ? Brushes.LightBlue : Brushes.Transparent);
            var borderBrush = _isHovered ? Brushes.DarkGray : Brushes.Gray;
            var textBrush = Brushes.Black;

            // Draw background
            context.FillRectangle(backgroundBrush, new Rect(0, 0, Width, Height));

            // Draw border
            context.DrawRectangle(null, new Pen(borderBrush, 1), new Rect(0, 0, Width, Height));

            // Draw text
            var formattedText = new FormattedText(
                Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                12,
                textBrush
            );

            var textPosition = new Point(
                (Width - formattedText.Width) / 2,
                (Height - formattedText.Height) / 2
            );

            context.DrawText(formattedText, textPosition);
        }

        private static void OnIsActiveChanged(object? sender, bool e)
        {
            if (sender is FilterButton filterButton)
            {
                filterButton.FilterToggled?.Invoke(filterButton, e);
            }
        }
    }
}