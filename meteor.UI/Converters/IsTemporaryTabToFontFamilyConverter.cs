using System.Globalization;
using meteor.UI.Interfaces;

namespace meteor.UI.Converters;

public class IsTemporaryTabToFontFamilyConverter : IValueConverter
{
    public object SanFrancisco { get; set; }
    public object SanFranciscoItalic { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isTemporary) return isTemporary ? SanFranciscoItalic : SanFrancisco;
        return SanFrancisco;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}