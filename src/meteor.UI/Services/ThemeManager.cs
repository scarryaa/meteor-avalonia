using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace meteor.UI.Services;

public class ThemeManager
{
    private readonly Avalonia.Application _application;
    private readonly Dictionary<string, ResourceDictionary> _themes = new();

    public event EventHandler<string> ThemeChanged;

    public ThemeManager(Avalonia.Application application)
    {
        _application = application;
    }

    public void AddTheme(string name, ResourceDictionary theme)
    {
        _themes[name] = theme;
    }

    public void SetTheme(string themeName)
    {
        if (_themes.TryGetValue(themeName, out var theme))
        {
            _application.Resources.MergedDictionaries.Clear();
            _application.Resources.MergedDictionaries.Add(theme);
            ThemeChanged?.Invoke(this, themeName);
        }
        else
        {
            throw new ArgumentException($"Theme '{themeName}' not found.");
        }
    }
}