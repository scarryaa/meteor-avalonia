using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

public interface IThemeManager
{
    ISettingsService SettingsService { get; set; }
    Theme CurrentTheme { get; set; }
    event EventHandler<Theme>? ThemeChanged;
    Theme GetTheme(string name);
    IEnumerable<string> GetAvailableThemes();
    void ApplyTheme(string themeName);
    void Initialize(ISettingsService settingsService);
}