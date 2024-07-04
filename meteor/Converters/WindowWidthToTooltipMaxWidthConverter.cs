using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace meteor.Converters;

public class WindowWidthToTooltipMaxWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double windowWidth)
        {
            // Get the factor from resources, or use a default value
            var factor = Application.Current.FindResource("TooltipMaxWidthFactor") as double? ?? 0.4;
            return Math.Max(500, Math.Min(windowWidth * factor, 600));
        }

        return 500; // Default fallback value
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}