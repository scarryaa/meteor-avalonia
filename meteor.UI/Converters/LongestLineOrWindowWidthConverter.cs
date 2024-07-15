using System.Globalization;
using meteor.UI.Interfaces;

namespace meteor.UI.Converters;

public class LongestLineOrWindowWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[0] == null || values[1] == null)
            return 0;
        
        if (values[0] is double longestLineWidth && values[1] is double windowWidth)
            return Math.Max(longestLineWidth + 20, windowWidth);

        return 0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}