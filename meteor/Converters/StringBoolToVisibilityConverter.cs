using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace meteor.Converters;

public class StringBoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == string.Empty || value == null) return false;

        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}