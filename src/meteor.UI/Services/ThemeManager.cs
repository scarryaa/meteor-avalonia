using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.UI.Interfaces;

namespace meteor.UI.Services;

public class ThemeManager : IThemeManager
{
    private readonly Application _application;
    private readonly Dictionary<string, ResourceDictionary> _themes = new();

    public ThemeManager(Application application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public event EventHandler<string>? ThemeChanged;
    
    public string CurrentTheme { get; set; }

    public void AddTheme(string name, ResourceDictionary theme)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Theme name cannot be null or whitespace.", nameof(name));
        if (theme == null)
            throw new ArgumentNullException(nameof(theme));

        _themes[name] = theme;
    }

    public void SetTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            throw new ArgumentException("Theme name cannot be null or whitespace.", nameof(themeName));

        if (_themes.TryGetValue(themeName, out var theme))
        {
            _application.Resources.MergedDictionaries.Clear();
            _application.Resources.MergedDictionaries.Add(theme);
            CurrentTheme = themeName;
            ThemeChanged?.Invoke(this, themeName);
        }
        else
        {
            throw new ArgumentException($"Theme '{themeName}' not found.", nameof(themeName));
        }
    }

    public ResourceDictionary GetTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            throw new ArgumentException("Theme name cannot be null or whitespace.", nameof(themeName));

        if (_themes.TryGetValue(themeName, out var theme)) return theme;

        throw new ArgumentException($"Theme '{themeName}' not found.", nameof(themeName));
    }

    public void LoadThemesFromJson(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Theme JSON file not found.", filePath);

        var jsonString = File.ReadAllText(filePath);
        var themeData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(jsonString);

        if (themeData == null)
            throw new JsonException("Failed to deserialize theme data.");

        if (!themeData.TryGetValue("Base", out var baseTheme))
            throw new JsonException("Base theme not found in theme data.");

        var baseResourceDictionary = new ResourceDictionary();
        foreach (var resource in baseTheme) AddResourceToDictionary(resource, baseResourceDictionary);

        foreach (var theme in themeData)
        {
            if (theme.Key == "Base")
                continue;

            var resourceDictionary = new ResourceDictionary();

            // Add base theme resources first
            foreach (var resource in baseResourceDictionary) resourceDictionary.Add(resource.Key, resource.Value);

            // Add specific theme resources, overriding base resources if necessary
            foreach (var resource in theme.Value) AddResourceToDictionary(resource, resourceDictionary);

            AddTheme(theme.Key, resourceDictionary);
        }

        foreach (var theme in _themes)
            Console.WriteLine($"Loaded theme: {theme.Key}");
    }

    public ResourceDictionary GetBaseTheme()
    {
        return GetTheme("Light");
    }

    private void AddResourceToDictionary(KeyValuePair<string, JsonElement> resource,
        ResourceDictionary resourceDictionary)
    {
        if (resource.Value.ValueKind == JsonValueKind.Number && resource.Value.TryGetInt32(out var intValue))
            resourceDictionary.Add(resource.Key, intValue);
        else if (TryParseColor(resource.Value.ToString(), out var color))
            resourceDictionary.Add(resource.Key, color);
        else if (resource.Key == "TabPadding" && TryParseThickness(resource.Value.ToString(), out var thickness))
            resourceDictionary.Add(resource.Key, thickness);
        else
            resourceDictionary.Add(resource.Key, resource.Value.ToString());
    }

    private bool TryParseThickness(string thicknessString, out Thickness thickness)
    {
        thickness = default;
        var parts = thicknessString.Split(',');
        if (parts.Length == 4 &&
            double.TryParse(parts[0], out var left) &&
            double.TryParse(parts[1], out var top) &&
            double.TryParse(parts[2], out var right) &&
            double.TryParse(parts[3], out var bottom))
        {
            thickness = new Thickness(left, top, right, bottom);
            return true;
        }

        return false;
    }

    private bool TryParseColor(string colorString, out Color color)
    {
        color = default;
        if (Color.TryParse(colorString, out var parsedColor))
        {
            color = parsedColor;
            return true;
        }

        return false;
    }
}
