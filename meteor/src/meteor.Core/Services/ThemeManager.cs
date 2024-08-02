using System.Reflection;
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
        _themesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
        LoadThemes();
        EnsureDefaultThemes();
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
        if (_themes.TryGetValue(name, out var theme)) return theme;
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

    private void LoadThemes()
    {
        LoadEmbeddedThemes();
        LoadFileSystemThemes();
    }

    private void LoadEmbeddedThemes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
            if (resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        var json = reader.ReadToEnd();
                        var theme = JsonSerializer.Deserialize(json, JsonContext.Default.Theme);
                        if (theme != null && !string.IsNullOrEmpty(theme.Name))
                        {
                            _themes[theme.Name] = theme;
                            Console.WriteLine($"Loaded embedded theme: {theme.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading embedded theme from {resourceName}: {ex.Message}");
                }
    }

    private void LoadFileSystemThemes()
    {
        if (!Directory.Exists(_themesDirectory)) Directory.CreateDirectory(_themesDirectory);

        foreach (var file in Directory.GetFiles(_themesDirectory, "*.json"))
            try
            {
                var json = File.ReadAllText(file);
                var theme = JsonSerializer.Deserialize(json, JsonContext.Default.Theme);
                if (theme != null && !string.IsNullOrEmpty(theme.Name))
                {
                    _themes[theme.Name] = theme;
                    Console.WriteLine($"Loaded file system theme: {theme.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading theme from {file}: {ex.Message}");
            }
    }

    private void EnsureDefaultThemes()
    {
        if (!_themes.ContainsKey("Dark"))
        {
            var darkTheme = CreateDarkTheme();
            _themes[darkTheme.Name] = darkTheme;
            SaveThemeToFile(darkTheme);
        }

        if (!_themes.ContainsKey("Light"))
        {
            var lightTheme = CreateLightTheme();
            _themes[lightTheme.Name] = lightTheme;
            SaveThemeToFile(lightTheme);
        }
    }

    private Theme CreateDarkTheme()
    {
        return new Theme
        {
            Name = "Dark",
            IsDark = true,
            BackgroundColor = "#121212",
            TextColor = "#E0E0E0",
            BorderBrush = "#2A2A2A",
            Background = "#121212",
            Foreground = "#E0E0E0",
            DirtyIndicatorBrush = "#FFD700",
            CloseButtonForeground = "#B0B0B0",
            CloseButtonBackground = "#1A1A1A",
            CurrentLineHighlightColor = "#1E1E1E",
            SelectionColor = "#3A5F8A",
            GutterBackgroundColor = "#121212",
            GutterTextColor = "#707070",
            KeywordColor = "#FF79C6",
            StringColor = "#F1FA8C",
            CommentColor = "#6272A4",
            NumberColor = "#BD93F9",
            OperatorColor = "#FF79C6",
            TypeColor = "#8BE9FD",
            MethodColor = "#50FA7B",
            PreprocessorColor = "#FF79C6",
            XmlDocColor = "#6272A4",
            AttributeColor = "#50FA7B",
            NamespaceColor = "#8BE9FD",
            LinqColor = "#FF79C6",
            LambdaColor = "#FF79C6",
            WhitespaceColor = "#282A36",
            TabBackgroundColor = "#1A1A1A",
            TabForegroundColor = "#808080",
            TabBorderColor = "#3A3A3A",
            TabActiveBackgroundColor = "#282A36",
            TabActiveForegroundColor = "#E0E0E0",
            AppBackgroundColor = "#121212",
            AppForegroundColor = "#E0E0E0",
            BackgroundBrush = "#121212",
            CompletionOverlayBackgroundBrush = "#1A1A1A",
            CompletionOverlayBorderBrush = "#2A2A2A",
            CompletionItemSelectedBrush = "#3A3A3A",
            TextBrush = "#E0E0E0",
            CompletionItemHoverBrush = "#1E1E1E",
            ScrollBarBackgroundBrush = "#0A0A0A",
            ScrollBarThumbBrush = "#3A3A3A",
            FileExplorerBackgroundColor = "#1E1E1E",
            FileExplorerForegroundColor = "#D4D4D4",
            FileExplorerSelectedItemBackgroundColor = "#3A3A3A",
            FileExplorerSelectedItemForegroundColor = "#FFFFFF",
            FileExplorerHoverItemBackgroundColor = "#2A2D2E",
            FileExplorerHoverItemForegroundColor = "#E9E9E9",
            FileExplorerFolderIconColor = "#D0D0D0",
            FileExplorerFileIconColor = "#C8C8C8",
            TitleBarColor = "#080808",
            TitleBarInactiveColor = "#181818",
            StatusBarColor = "#121212",
            ButtonHoverColor = "#2A2A2A",
            ButtonPressedColor = "#3A3A3A",
            ButtonActiveColor = "#4A4A4A"
        };
    }

    private Theme CreateLightTheme()
    {
        return new Theme
        {
            Name = "Light",
            IsDark = false,
            BackgroundColor = "#FFFFFF",
            TextColor = "#2C3E50",
            BorderBrush = "#C0C0C0",
            Background = "#FFFFFF",
            Foreground = "#2C3E50",
            DirtyIndicatorBrush = "#F39C12",
            CloseButtonForeground = "#7F8C8D",
            CloseButtonBackground = "#ECF0F1",
            CurrentLineHighlightColor = "#ECF0F1",
            SelectionColor = "#BDE0F7",
            GutterBackgroundColor = "#FFFFFF",
            GutterTextColor = "#5D6D7E",
            KeywordColor = "#3498DB",
            StringColor = "#27AE60",
            CommentColor = "#95A5A6",
            NumberColor = "#E74C3C",
            OperatorColor = "#2C3E50",
            TypeColor = "#16A085",
            MethodColor = "#8E44AD",
            PreprocessorColor = "#7F8C8D",
            XmlDocColor = "#7F8C8D",
            AttributeColor = "#D35400",
            NamespaceColor = "#2C3E50",
            LinqColor = "#3498DB",
            LambdaColor = "#3498DB",
            WhitespaceColor = "#ECF0F1",
            TabBackgroundColor = "#ECF0F1",
            TabForegroundColor = "#2C3E50",
            TabBorderColor = "#BDC3C7",
            TabActiveBackgroundColor = "#FFFFFF",
            TabActiveForegroundColor = "#2C3E50",
            AppBackgroundColor = "#F5F7FA",
            AppForegroundColor = "#2C3E50",
            BackgroundBrush = "#FFFFFF",
            CompletionOverlayBackgroundBrush = "#F5F5F5",
            CompletionOverlayBorderBrush = "#B0B0B0",
            CompletionItemSelectedBrush = "#E0E0E0",
            TextBrush = "#2C3E50",
            CompletionItemHoverBrush = "#ECF0F1",
            ScrollBarBackgroundBrush = "#ECF0F1",
            ScrollBarThumbBrush = "#BDC3C7",
            FileExplorerBackgroundColor = "#F5F7FA",
            FileExplorerForegroundColor = "#2C3E50",
            FileExplorerSelectedItemBackgroundColor = "#E0E0E0",
            FileExplorerSelectedItemForegroundColor = "#2C3E50",
            FileExplorerHoverItemBackgroundColor = "#ECF0F1",
            FileExplorerHoverItemForegroundColor = "#2C3E50",
            FileExplorerFolderIconColor = "#A0A0A0",
            FileExplorerFileIconColor = "#B0B0B0",
            TitleBarColor = "#C0C0C0",
            TitleBarInactiveColor = "#E0E0E0",
            StatusBarColor = "#FFFFFF",
            ButtonHoverColor = "#E0E0E0",
            ButtonPressedColor = "#D0D0D0",
            ButtonActiveColor = "#C0C0C0"
        };
    }

    private void SaveThemeToFile(Theme theme)
    {
        var filePath = Path.Combine(_themesDirectory, $"{theme.Name}.json");
        var json = JsonSerializer.Serialize(theme, JsonContext.Default.Theme);
        File.WriteAllText(filePath, json);
        Console.WriteLine($"Created theme file: {filePath}");
    }

    private void LoadCurrentThemeFromSettings()
    {
        if (SettingsService == null)
            throw new InvalidOperationException("SettingsService is not set. Call Initialize first.");

        var themeName = SettingsService.GetSetting("CurrentTheme", "Dark");
        CurrentTheme = GetTheme(themeName);
        ThemeChanged?.Invoke(this, CurrentTheme);
    }
}