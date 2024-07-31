using System.ComponentModel;
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
    private readonly IThemeManager _themeManager;

    public TabConfig(ITabService tabService, IThemeManager themeManager)
    {
        _tabService = tabService;
        _themeManager = themeManager;
        CloseTabCommand = new RelayCommand<ITabViewModel>(CloseTab);
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    public ISolidColorBrush BorderBrush =>
        new SolidColorBrush(Color.FromHex(_themeManager.CurrentTheme.TabBorderColor));

    public ISolidColorBrush Background =>
        new SolidColorBrush(Color.FromHex(_themeManager.CurrentTheme.TabBackgroundColor));

    public ISolidColorBrush DirtyIndicatorBrush =>
        new SolidColorBrush(Color.FromHex(_themeManager.CurrentTheme.DirtyIndicatorBrush));

    public ISolidColorBrush Foreground =>
        new SolidColorBrush(Color.FromHex(_themeManager.CurrentTheme.TabForegroundColor));

    public ISolidColorBrush CloseButtonForeground =>
        new SolidColorBrush(Color.FromHex(_themeManager.CurrentTheme.CloseButtonForeground));

    public ISolidColorBrush CloseButtonBackground =>
        new SolidColorBrush(Color.FromHex(_themeManager.CurrentTheme.CloseButtonBackground));

    public ICommand CloseTabCommand { get; }

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

    private void CloseTab(ITabViewModel? tab)
    {
        if (tab != null) _tabService.RemoveTab(tab);
    }

    private void OnThemeChanged(object? sender, Theme e)
    {
        OnPropertyChanged(nameof(BorderBrush));
        OnPropertyChanged(nameof(Background));
        OnPropertyChanged(nameof(DirtyIndicatorBrush));
        OnPropertyChanged(nameof(Foreground));
        OnPropertyChanged(nameof(CloseButtonForeground));
        OnPropertyChanged(nameof(CloseButtonBackground));
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}