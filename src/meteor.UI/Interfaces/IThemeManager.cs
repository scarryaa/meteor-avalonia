using System;
using Avalonia.Controls;

namespace meteor.UI.Interfaces;

public interface IThemeManager
{
    event EventHandler<string> ThemeChanged;

    string CurrentTheme { get; }

    void AddTheme(string name, ResourceDictionary theme);
    void SetTheme(string themeName);
    ResourceDictionary GetTheme(string themeName);
}