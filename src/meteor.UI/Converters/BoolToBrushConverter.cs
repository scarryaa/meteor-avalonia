using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace meteor.UI.Converters;

public class BoolToBrushConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Count == 3 && values[0] is bool isActive && values[1] is IBrush inactiveBrush &&
            values[2] is IBrush activeBrush) return isActive ? activeBrush : inactiveBrush;

        return Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}