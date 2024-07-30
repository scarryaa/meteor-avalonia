using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace meteor.UI.Converters;

public class IsTemporaryTabToFontStyleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isTemporary) return isTemporary ? FontStyle.Italic : FontStyle.Normal;

        return FontStyle.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}