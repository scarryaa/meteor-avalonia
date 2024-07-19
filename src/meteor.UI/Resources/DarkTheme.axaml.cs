using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace meteor.UI.Resources;

public class DarkTheme : ResourceDictionary
{
    public DarkTheme()
    {
        AvaloniaXamlLoader.Load(this);
    }
}