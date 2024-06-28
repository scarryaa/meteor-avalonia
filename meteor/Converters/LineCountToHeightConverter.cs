using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace meteor.Converters;

public class LineCountToHeightConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        const int verticalPadding = 5;
        if (values is [int lineCount, double lineHeight, double minHeight])
        {
            var calculatedHeight = lineCount * lineHeight;
            return Math.Max(calculatedHeight, minHeight) + verticalPadding;
        }

        return 0;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}