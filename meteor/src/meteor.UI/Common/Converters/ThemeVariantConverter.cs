using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using meteor.Core.Models;

namespace meteor.UI.Common.Converters;

public class ThemeVariantConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Theme theme) return theme.IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
        return ThemeVariant.Default;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}