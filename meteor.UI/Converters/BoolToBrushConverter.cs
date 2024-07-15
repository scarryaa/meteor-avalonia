using System.Globalization;
using meteor.UI.Interfaces;

namespace meteor.UI.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && parameter is string colors)
        {
            var colorParts = colors.Split(';');
            return isSelected ? colorParts[1] : colorParts[0];
        }

        return "Transparent";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}