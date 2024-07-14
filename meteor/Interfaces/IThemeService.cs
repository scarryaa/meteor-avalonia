using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace meteor.Interfaces;

public interface IThemeService
{
    void SetTheme(string themeSource);
    IResourceProvider GetCurrentTheme();
    IBrush GetResourceBrush(string key);
    event EventHandler ThemeChanged;
}