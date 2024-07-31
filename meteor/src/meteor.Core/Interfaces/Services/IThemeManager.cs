using meteor.Core.Models;

public interface IThemeManager
{
    Theme CurrentTheme { get; set; }
    event EventHandler<Theme>? ThemeChanged;
    Theme GetTheme(string name);
    IEnumerable<string> GetAvailableThemes();
    void ApplyTheme(string themeName);
}