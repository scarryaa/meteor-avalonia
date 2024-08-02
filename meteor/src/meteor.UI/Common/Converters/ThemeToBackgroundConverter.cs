using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using meteor.Core.Models;
using Color = Avalonia.Media.Color;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;

namespace meteor.UI.Common.Converters
{
    public class ThemeToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Theme theme)
            {
                return new SolidColorBrush(Color.Parse(theme.BackgroundColor));
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
