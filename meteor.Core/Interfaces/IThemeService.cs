using meteor.Core.Interfaces.Resources;

namespace meteor.Core.Interfaces;

public interface IThemeService
{
    void SetTheme(string themeSource);
    IResourceProvider GetCurrentTheme();
    object GetResource(string key);
    event EventHandler ThemeChanged;
    object GetResourceBrush(string key);
}