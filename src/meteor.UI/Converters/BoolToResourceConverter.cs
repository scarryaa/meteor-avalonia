using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace meteor.UI.Converters;

public class BoolToResourceConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is [bool isActive, string inactiveKey, string activeKey, Control control])
        {
            var resourceKey = isActive ? activeKey : inactiveKey;

            if (control.FindResource(resourceKey) is { } resource) return resource;
        }

        return AvaloniaProperty.UnsetValue;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}