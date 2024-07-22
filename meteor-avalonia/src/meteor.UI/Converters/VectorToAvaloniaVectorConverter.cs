using System;
using System.Globalization;
using Avalonia.Data.Converters;
using meteor.Core.Models;

namespace meteor.UI.Converters;

public class VectorToAvaloniaVectorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Vector v) return new Avalonia.Vector(v.X, v.Y);
        return new Avalonia.Vector();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Avalonia.Vector av) return new Vector(av.X, av.Y);
        return new Vector();
    }
}