using System.Text.Json;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class ThemeManager : IThemeManager
{
    private readonly string _themesDirectory;
    private readonly Dictionary<string, Theme> _themes = new();

    public Theme CurrentTheme { get; set; }

    public ThemeManager(string themesDirectory)
    {
        _themesDirectory = themesDirectory;
        LoadThemes();
        CurrentTheme = GetTheme("Dark");
    }

    private void LoadThemes()
    {
        foreach (var file in Directory.GetFiles(_themesDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var theme = JsonSerializer.Deserialize<Theme>(json);
                if (theme != null && !string.IsNullOrEmpty(theme.Name))
                {
                    _themes[theme.Name] = theme;
                }
            }
            catch (Exception ex)
            {
                // Log the error or handle it as appropriate for your application
                Console.WriteLine($"Error loading theme from {file}: {ex.Message}");
            }
        }
    }

    public Theme GetTheme(string name)
    {
        return _themes.TryGetValue(name, out var theme) ? theme : _themes["Dark"];
    }

    public IEnumerable<string> GetAvailableThemes()
    {
        return _themes.Keys;
    }

    public void ApplyTheme(string themeName)
    {
        if (_themes.TryGetValue(themeName, out var theme))
        {
            CurrentTheme = theme;
            ThemeChanged?.Invoke(this, theme);
        }
        else
        {
            throw new ArgumentException($"Theme '{themeName}' not found.");
        }
    }

    public event EventHandler<Theme>? ThemeChanged;
}