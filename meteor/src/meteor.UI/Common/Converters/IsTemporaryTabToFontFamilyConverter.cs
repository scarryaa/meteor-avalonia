using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace meteor.UI.Common.Converters;

public class IsTemporaryTabToFontFamilyConverter : IValueConverter
{
    public FontFamily SanFrancisco { get; set; } = new("San Francisco");
    public FontFamily SanFranciscoItalic { get; set; } = new("San Francisco");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isTemporary) return isTemporary ? SanFranciscoItalic : SanFrancisco;

        return SanFrancisco;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}