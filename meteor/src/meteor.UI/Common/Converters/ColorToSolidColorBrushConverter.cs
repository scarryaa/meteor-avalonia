using System.Globalization;
using Avalonia.Data.Converters;
using Color = meteor.Core.Models.Color;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;

namespace meteor.UI.Common.Converters
{
    public class ColorToSolidColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(new Avalonia.Media.Color(color.A, color.R, color.G, color.B));
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return new Color(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
            }

            return null;
        }
    }
}
