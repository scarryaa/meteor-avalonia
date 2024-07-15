using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Resources;
using meteor.Core.Models.Resources;
using Microsoft.Extensions.Logging;

namespace meteor.Services;

public class ThemeService : IThemeService
{
    private readonly IApplicationResourceProvider _applicationResourceProvider;
    private IResourceProvider _currentTheme;
    private readonly ILogger<ThemeService> _logger;

    public event EventHandler ThemeChanged;

    public object GetResourceBrush(string key)
    {
        return _currentTheme.GetResource(key);
    }

    public ThemeService(IApplicationResourceProvider applicationResourceProvider, ILogger<ThemeService> logger)
    {
        _logger = logger;
        _logger.LogDebug("Initializing ThemeService");
        
        _applicationResourceProvider = applicationResourceProvider;
        _currentTheme = _applicationResourceProvider.Resources;
    }

    public void SetTheme(string themeSource)
    {
        try
        {
            var newTheme = new ResourceDictionary();

            var themeResources = _applicationResourceProvider.LoadResource(themeSource);
            newTheme.MergedDictionaries.Add(themeResources);

            _applicationResourceProvider.Resources = newTheme;
            _currentTheme = newTheme;

            _logger.LogDebug("Theme set successfully.");
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error setting theme from source {themeSource}: {ex.Message}");
        }
    }

    public object GetResource(string key)
    {
        return GetResourceInternal(_currentTheme, key);
    }

    private object GetResourceInternal(IResourceProvider resources, string key)
    {
        if (resources == null)
        {
            _logger.LogWarning($"Warning: ResourceProvider is null when trying to get {key}");
            return null;
        }

        if (TryGetResourceRecursive(resources, key, out var resource)) return resource;

        _logger.LogWarning($"Warning: Resource {key} not found in the theme");
        return null;
    }

    private bool TryGetResourceRecursive(IResourceProvider resources, string key, out object result)
    {
        if (resources.TryGetResource(key, out result)) return true;

        if (resources is ResourceDictionary resourceDictionary)
            foreach (var dict in resourceDictionary.MergedDictionaries)
                if (TryGetResourceRecursive(dict, key, out result))
                    return true;

        result = null;
        return false;
    }

    public IResourceProvider GetCurrentTheme()
    {
        return _currentTheme ?? _applicationResourceProvider.Resources;
    }
}