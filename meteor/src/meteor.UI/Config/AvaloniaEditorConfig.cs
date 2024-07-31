using System.ComponentModel;
using Avalonia.Media;
using meteor.Core.Config;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Config;

public class AvaloniaEditorConfig : EditorConfig, INotifyPropertyChanged
{
    private readonly IThemeManager _themeManager;

    public AvaloniaEditorConfig(IThemeManager themeManager)
    {
        _themeManager = themeManager;
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    public Typeface Typeface =>
        new("avares://meteor.UI/Common/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono");

    public IBrush TextBrush => new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor));

    public IBrush BackgroundBrush => new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor));

    public IBrush CurrentLineHighlightBrush => new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.CurrentLineHighlightColor));

    public IBrush SelectionBrush => new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.SelectionColor));

    public IBrush GutterBackgroundBrush => new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.GutterBackgroundColor));

    public IBrush GutterTextBrush => new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.GutterTextColor));

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnThemeChanged(object? sender, Core.Models.Theme e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextBrush)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackgroundBrush)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLineHighlightBrush)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionBrush)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GutterBackgroundBrush)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GutterTextBrush)));
    }
}