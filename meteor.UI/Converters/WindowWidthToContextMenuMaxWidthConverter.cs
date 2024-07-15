using System.Globalization;
using meteor.Core.Interfaces.Resources;
using meteor.UI.Interfaces;

namespace meteor.UI.Converters;

public class WindowWidthToContextMenuMaxWidthConverter : IValueConverter
{
    private IResourceProvider _resourceProvider;

    public WindowWidthToContextMenuMaxWidthConverter()
    {
        // Default constructor
    }

    public WindowWidthToContextMenuMaxWidthConverter(IResourceProvider resourceProvider)
    {
        _resourceProvider = resourceProvider;
    }

    public void SetResourceProvider(IResourceProvider resourceProvider)
    {
        _resourceProvider = resourceProvider;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (_resourceProvider == null) return 500;

        if (value is double windowWidth)
        {
            var factor = _resourceProvider.GetResource("ContextMenuMaxWidthFactor") as double? ?? 0.4;
            return Math.Max(500, Math.Min(windowWidth * factor, 600));
        }
        return 500; // Default fallback value
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}