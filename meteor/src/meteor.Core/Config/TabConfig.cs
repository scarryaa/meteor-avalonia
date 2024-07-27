using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Models;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;

namespace meteor.Core.Config;

public class TabConfig : ITabViewModelConfig
{
    private readonly ITabService _tabService;
    private bool _isDarkMode;
    public ISolidColorBrush BorderBrush => GetColor(0xFFA0A0A0, 0xFF3C3C3C);
    public ISolidColorBrush Background => GetColor(0xFFF5F5F5, 0xFF2D2D2D);
    public ISolidColorBrush DirtyIndicatorBrush => GetColor(0xFFFFA500, 0xFFFFD700);
    public ISolidColorBrush Foreground => GetColor(0xFF333333, 0xFFE0E0E0);
    public ISolidColorBrush CloseButtonForeground => GetColor(0xFF666666, 0xFFAAAAAA);
    public ISolidColorBrush CloseButtonBackground => GetColor(0xFFC0C0C0, 0xFF3C3C3C);

    public ICommand CloseTabCommand { get; }

    public TabConfig(ITabService tabService)
    {
        _tabService = tabService;
        CloseTabCommand = new RelayCommand<ITabViewModel>(CloseTab);
    }

    private void CloseTab(ITabViewModel? tab)
    {
        if (tab != null) _tabService.RemoveTab(tab);
    }

    public ISolidColorBrush GetBorderBrush()
    {
        return BorderBrush;
    }

    public ISolidColorBrush GetBackground()
    {
        return Background;
    }

    public ISolidColorBrush GetDirtyIndicatorBrush()
    {
        return DirtyIndicatorBrush;
    }

    public ISolidColorBrush GetForeground()
    {
        return Foreground;
    }

    public ISolidColorBrush GetCloseButtonForeground()
    {
        return CloseButtonForeground;
    }

    public ISolidColorBrush GetCloseButtonBackground()
    {
        return CloseButtonBackground;
    }

    public ICommand GetCloseTabCommand()
    {
        return CloseTabCommand;
    }

    private ISolidColorBrush GetColor(uint lightColor, uint darkColor)
    {
        var color = _isDarkMode ? darkColor : lightColor;
        var a = (byte)((color >> 24) & 0xFF);
        var r = (byte)((color >> 16) & 0xFF);
        var g = (byte)((color >> 8) & 0xFF);
        var b = (byte)(color & 0xFF);
        return new SolidColorBrush(Color.FromArgb(a, r, g, b));
    }
}