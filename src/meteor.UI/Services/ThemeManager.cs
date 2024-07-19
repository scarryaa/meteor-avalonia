using System;
using System.Collections.Generic;
using Avalonia.Controls;
using meteor.UI.Interfaces;

namespace meteor.UI.Services;

public class ThemeManager : IThemeManager
{
    private readonly Avalonia.Application _application;
    private readonly Dictionary<string, ResourceDictionary> _themes = new();

    public event EventHandler<string>? ThemeChanged;

    public ThemeManager(Avalonia.Application application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

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
}