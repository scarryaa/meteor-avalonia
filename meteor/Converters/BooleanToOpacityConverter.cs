using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace meteor.Converters;

public class BooleanToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isVisible && isVisible ? 1.0 : 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}