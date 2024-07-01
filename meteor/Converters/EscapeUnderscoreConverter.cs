using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace meteor.Converters;

public class EscapeUnderscoreConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue) return stringValue.Replace("_", "__");

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}