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
        if (values is [long lineCount, double lineHeight, double minHeight])
        {
            // Calculate the height using the adjusted line height
            var adjustedLineHeight = lineHeight * 1.5;
            var calculatedHeight = lineCount * adjustedLineHeight;
            // Hacky method to fix text rendering below the viewport
            calculatedHeight = (double)lineCount * lineHeight;
            return Math.Max(calculatedHeight + verticalPadding, minHeight);
        }

        return 0;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}