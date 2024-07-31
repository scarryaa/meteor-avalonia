using System.Text.Json;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class ThemeManager : IThemeManager
{
    private static ThemeManager? _instance;
    private readonly string _themesDirectory;
    private readonly Dictionary<string, Theme> _themes = new();

    public Theme CurrentTheme { get; set; }

    public static ThemeManager Instance => _instance ??= new ThemeManager();

    public ThemeManager()
    {
        _themesDirectory = GetDefaultThemesDirectory();
        LoadThemes();
        CurrentTheme = GetTheme("Dark");
    }

    private string GetDefaultThemesDirectory()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../meteor.UI/Common/Themes");
    }

    private void LoadThemes()
    {
        if (!Directory.Exists(_themesDirectory))
        {
            Directory.CreateDirectory(_themesDirectory);
        }

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