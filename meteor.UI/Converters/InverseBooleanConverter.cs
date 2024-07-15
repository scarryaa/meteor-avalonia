using System.Globalization;
using meteor.UI.Interfaces;

namespace meteor.UI.Converters;

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool booleanValue) return !booleanValue;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool booleanValue) return !booleanValue;
        return false;
    }
}