using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace meteor.Converters;

public class LineCountToHeightConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 3 && values[0] is int lineCount && values[1] is double lineHeight &&
            values[2] is double minHeight)
        {
            var calculatedHeight = lineCount * lineHeight;
            return Math.Max(calculatedHeight, minHeight);
        }

        return 0;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}