using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using meteor.Interfaces;

namespace meteor.Services;

public class ThemeService : IThemeService
{
    private readonly Application _application;
    private IResourceProvider _currentTheme;

    public event EventHandler ThemeChanged;

    public ThemeService(Application application)
    {
        _application = application;
        _currentTheme = _application.Resources;
    }

    public void SetTheme(string themeSource)
    {
        try
        {
            var newTheme = new ResourceDictionary();

            var baseResources = new ResourceInclude(new Uri("avares://meteor/Resources"))
            {
                Source = new Uri("avares://meteor/Resources/BaseResources.axaml")
            };
            newTheme.MergedDictionaries.Add(baseResources);

            var themeResources = new ResourceInclude(new Uri("avares://meteor/Resources"))
            {
                Source = new Uri(themeSource)
            };
            newTheme.MergedDictionaries.Add(themeResources);

            _application.Resources = newTheme;
            _currentTheme = newTheme;

            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting theme from source {themeSource}: {ex.Message}");
        }
    }

    public IBrush GetResourceBrush(string key)
    {
        return GetResourceBrush(_currentTheme, key);
    }

    private IBrush GetResourceBrush(IResourceProvider resources, string key)
    {
        if (resources == null)
        {
            Console.WriteLine($"Warning: ResourceProvider is null when trying to get {key}");
            return null;
        }

        if (TryGetResourceRecursive(resources, key, out var resource))
        {
            if (resource is IBrush brush)
                return brush;
            if (resource is Color color)
                return new SolidColorBrush(color);
            Console.WriteLine($"Warning: Resource {key} is not an IBrush or Color");
        }
        else
        {
            Console.WriteLine($"Warning: Resource {key} not found in the theme");
        }

        return null;
    }

    private bool TryGetResourceRecursive(IResourceProvider resources, string key, out object result)
    {
        if (resources.TryGetResource(key, null, out result)) return true;

        if (resources is IResourceDictionary resourceDictionary)
            foreach (var dict in resourceDictionary.MergedDictionaries)
                if (TryGetResourceRecursive(dict, key, out result))
                    return true;

        result = null;
        return false;
    }

    public IResourceProvider GetCurrentTheme()
    {
        return _currentTheme ?? _application.Resources;
    }
}