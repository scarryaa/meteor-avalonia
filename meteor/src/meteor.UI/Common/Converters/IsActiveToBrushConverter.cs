using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace meteor.UI.Common.Converters;

public class IsActiveToBrushConverter : IValueConverter
{
    public IBrush ActiveBrush { get; set; }
    public IBrush InactiveBrush { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isActive) return isActive ? ActiveBrush : InactiveBrush;
        return InactiveBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}