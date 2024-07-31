using System.Text.Json;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class ThemeManager : IThemeManager
{
    private static ThemeManager? _instance;
    private readonly Dictionary<string, Theme> _themes = new();
    private readonly string _themesDirectory;

    private ThemeManager()
    {
        _themesDirectory = GetThemesDirectory();
        LoadThemes();
        CurrentTheme = GetTheme("Dark") ?? CreateDefaultTheme();
    }

    public static ThemeManager Instance => _instance ??= new ThemeManager();

    public ISettingsService SettingsService { get; set; }
    public Theme CurrentTheme { get; set; }

    public void Initialize(ISettingsService settingsService)
    {
        SettingsService = settingsService;
        LoadCurrentThemeFromSettings();
    }

    public Theme GetTheme(string name)
    {
        if (_themes.TryGetValue(name, out var theme))
        {
            return theme;
        }
        Console.WriteLine($"Theme '{name}' not found. Using default theme.");
        return _themes.Values.First(); // Return the first available theme
    }

    public IEnumerable<string> GetAvailableThemes()
    {
        return _themes.Keys;
    }

    public void ApplyTheme(string themeName)
    {
        if (SettingsService == null)
            throw new InvalidOperationException("SettingsService is not set. Call Initialize first.");

        if (_themes.TryGetValue(themeName, out var theme))
        {
            CurrentTheme = theme;
            SettingsService.SetSetting("CurrentTheme", themeName);
            SettingsService.SaveSettings();
            ThemeChanged?.Invoke(this, theme);
        }
        else
        {
            throw new ArgumentException($"Theme '{themeName}' not found.");
        }
    }

    public event EventHandler<Theme>? ThemeChanged;

    private string GetThemesDirectory()
    {
        string[] possiblePaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Common", "Themes"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes"),
            Path.Combine(AppContext.BaseDirectory, "Common", "Themes"),
            Path.Combine(AppContext.BaseDirectory, "Themes"),
            Path.Combine(Environment.CurrentDirectory, "Common", "Themes"),
            Path.Combine(Environment.CurrentDirectory, "Themes")
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                Console.WriteLine($"Themes directory found: {path}");
                return path;
            }
        }

        Console.WriteLine("Themes directory not found in any of the expected locations.");
        return string.Empty;
    }

    private void LoadThemes()
    {
        if (string.IsNullOrEmpty(_themesDirectory))
        {
            Console.WriteLine("No themes directory found. Creating default theme.");
            var defaultTheme = CreateDefaultTheme();
            _themes[defaultTheme.Name] = defaultTheme;
            return;
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
                    Console.WriteLine($"Loaded theme: {theme.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading theme from {file}: {ex.Message}");
            }
        }

        if (_themes.Count == 0)
        {
            Console.WriteLine("No themes were loaded. Creating default theme.");
            var defaultTheme = CreateDefaultTheme();
            _themes[defaultTheme.Name] = defaultTheme;
        }
    }

    private Theme CreateDefaultTheme()
    {
        return new Theme
        {
            Name = "Default",
            BackgroundColor = "#252526",
            TextColor = "#D4D4D4",
        };
    }

    public void LoadCurrentThemeFromSettings()
    {
        if (SettingsService == null)
            throw new InvalidOperationException("SettingsService is not set. Call Initialize first.");

        var themeName = SettingsService.GetSetting("CurrentTheme", "Dark");
        CurrentTheme = GetTheme(themeName);
        ThemeChanged?.Invoke(this, CurrentTheme);
    }
}