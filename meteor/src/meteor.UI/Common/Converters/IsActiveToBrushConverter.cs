using System.Globalization;
using Avalonia.Data.Converters;

namespace meteor.UI.Common.Converters;

public class IsActiveToBrushConverter : IValueConverter
{
    private static IThemeManager _themeManager;

    public static void Initialize(IThemeManager themeManager)
    {
        _themeManager = themeManager;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (_themeManager == null) throw new InvalidOperationException("ThemeManager has not been initialized.");

        var isActive = (bool)value;
        return isActive
            ? _themeManager.CurrentTheme.TabActiveBackgroundColor
            : _themeManager.CurrentTheme.TabBackgroundColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}