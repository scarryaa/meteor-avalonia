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
        _themesDirectory = GetDefaultThemesDirectory();
        LoadThemes();
        CurrentTheme = GetTheme("Dark");
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
        return _themes.TryGetValue(name, out var theme) ? theme : _themes["Dark"];
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

    private string GetDefaultThemesDirectory()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../meteor.UI/Common/Themes");
    }

    private void LoadThemes()
    {
        if (!Directory.Exists(_themesDirectory)) Directory.CreateDirectory(_themesDirectory);

        foreach (var file in Directory.GetFiles(_themesDirectory, "*.json"))
            try
            {
                var json = File.ReadAllText(file);
                var theme = JsonSerializer.Deserialize<Theme>(json);
                if (theme != null && !string.IsNullOrEmpty(theme.Name)) _themes[theme.Name] = theme;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading theme from {file}: {ex.Message}");
            }
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