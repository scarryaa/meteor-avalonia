using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace meteor.UI.Resources;

public class LightTheme : ResourceDictionary
{
    public LightTheme()
    {
        AvaloniaXamlLoader.Load(this);
    }
}