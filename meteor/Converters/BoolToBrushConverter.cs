using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace meteor.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && parameter is string colors)
        {
            var colorParts = colors.Split(';');
            return isSelected ? Brush.Parse(colorParts[1]) : Brush.Parse(colorParts[0]);
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}