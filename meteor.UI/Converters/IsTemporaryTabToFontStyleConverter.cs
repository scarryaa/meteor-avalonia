using System.Globalization;
using meteor.UI.Interfaces;

namespace meteor.UI.Converters;

public class IsTemporaryTabToFontStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isTemporary) return isTemporary ? "Italic" : "Normal";
        return "Normal";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}