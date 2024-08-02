using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using meteor.Core.Models;
using Color = Avalonia.Media.Color;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;

namespace meteor.UI.Common.Converters
{
    public class ThemeToCloseButtonForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Theme theme)
            {
                return new SolidColorBrush(Color.Parse(theme.AppForegroundColor));
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
