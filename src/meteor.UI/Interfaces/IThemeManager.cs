using System;
using Avalonia.Controls;

namespace meteor.UI.Interfaces;

public interface IThemeManager
{
    string CurrentTheme { get; }
    event EventHandler<string> ThemeChanged;

    void AddTheme(string name, ResourceDictionary theme);
    void SetTheme(string themeName);
    ResourceDictionary GetTheme(string themeName);
}