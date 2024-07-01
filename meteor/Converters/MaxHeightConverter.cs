using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace meteor.Converters
{
    public class MaxHeightConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count != 2 || values[0] == null || values[1] == null)
                return 0;

            if (values[0] is double totalHeight && values[1] is double windowHeight)
                return Math.Max(totalHeight, windowHeight);

            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}