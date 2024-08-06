using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using meteor.Core.Models;
using Color = Avalonia.Media.Color;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;

namespace meteor.UI.Features.StatusBar.Controls;

public class StatusBar : UserControl
{
    private readonly IThemeManager _themeManager;
    private Border _border;
    private TextBlock _lineColumnTextBlock;
    private TextBlock _vimModeTextBlock;
    private Button _leftSidebarToggleButton;
    private Button _rightSidebarToggleButton;

    public event EventHandler<(int Line, int Column)> GoToLineColumnRequested;
    public event EventHandler LeftSidebarToggleRequested;
    public event EventHandler RightSidebarToggleRequested;

    public StatusBar(IThemeManager themeManager)
    {
        _themeManager = themeManager;
        InitializeComponent();
        ApplyTheme();
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    private void InitializeComponent()
    {
        Height = 25;

        _leftSidebarToggleButton = CreateSidebarToggleButton("\uf0c9", HorizontalAlignment.Left);
        _rightSidebarToggleButton = CreateSidebarToggleButton("\uf0c9", HorizontalAlignment.Right);

        _leftSidebarToggleButton.Click += (_, _) => LeftSidebarToggleRequested?.Invoke(this, EventArgs.Empty);
        _rightSidebarToggleButton.Click += (_, _) => RightSidebarToggleRequested?.Invoke(this, EventArgs.Empty);

        _vimModeTextBlock = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 10, 0),
            IsVisible = false,
        };

        _lineColumnTextBlock = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10, 0, 10, 0),
            Cursor = new Cursor(StandardCursorType.Hand),
            IsVisible = true
        };

        _lineColumnTextBlock.PointerPressed += OnLineColumnTextBlockPressed;

        _border = new Border
        {
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto"),
                Children =
                {
                    new Panel { Children = { _leftSidebarToggleButton }, [Grid.ColumnProperty] = 0 },
                    new Panel { Children = { _vimModeTextBlock }, [Grid.ColumnProperty] = 2 },
                    new Panel { Children = { _lineColumnTextBlock }, [Grid.ColumnProperty] = 3 },
                    new Panel { Children = { _rightSidebarToggleButton }, [Grid.ColumnProperty] = 4 }
                }
            },
            BorderThickness = new Thickness(0, 1, 0, 0)
        };

        Content = _border;

        // Set initial content for TextBlocks to ensure they are visible
        SetLineAndColumn(1, 1);
    }

    private Button CreateSidebarToggleButton(string icon, HorizontalAlignment alignment) =>
        new()
        {
            Content = new TextBlock
            {
                Text = icon,
                FontFamily = new FontFamily("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free"),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            Width = 25,
            Height = 25,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = alignment,
            Margin = new Thickness(0),
            Classes = { "sidebar-toggle", "sidebar-open" }
        };

    private void UpdateButtonStyles(Button button)
    {
        var theme = _themeManager.CurrentTheme;
        button.Styles.Clear();
        button.Styles.Add(CreateButtonStyle("sidebar-toggle", Brushes.Transparent));
        button.Styles.Add(CreateButtonStyle("sidebar-toggle:pointerover", new SolidColorBrush(Color.Parse(theme.ButtonHoverColor))));
        button.Styles.Add(CreateButtonStyle("sidebar-toggle:pressed", new SolidColorBrush(Color.Parse(theme.ButtonPressedColor))));
        button.Styles.Add(CreateButtonStyle("sidebar-toggle.sidebar-open", new SolidColorBrush(Color.Parse(theme.ButtonActiveColor))));
    }

    private Style CreateButtonStyle(string selector, IBrush background) =>
        new(x => x.OfType<Button>().Class(selector))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, background),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
            }
        };

    private void ApplyTheme()
    {
        var theme = _themeManager.CurrentTheme;
        Background = new SolidColorBrush(Color.Parse(theme.StatusBarColor));
        _lineColumnTextBlock.Foreground = _vimModeTextBlock.Foreground = _leftSidebarToggleButton.Foreground = _rightSidebarToggleButton.Foreground = new SolidColorBrush(Color.Parse(theme.TextBrush));
        _border.BorderBrush = new SolidColorBrush(Color.Parse(theme.BorderBrush));
        UpdateButtonStyles(_leftSidebarToggleButton);
        UpdateButtonStyles(_rightSidebarToggleButton);
    }

    private void OnThemeChanged(object sender, Theme theme) => ApplyTheme();

    private void OnLineColumnTextBlockPressed(object sender, PointerPressedEventArgs e)
    {
        var parts = _lineColumnTextBlock.Text.Split(',');
        if (parts.Length == 2 &&
            int.TryParse(parts[0].Split(' ')[1], out var line) &&
            int.TryParse(parts[1].Split(' ')[2], out var column))
            GoToLineColumnRequested?.Invoke(this, (line, column));
    }

    public void SetLineAndColumn(int line, int column)
    {
        _lineColumnTextBlock.Text = $"Ln {line}, Col {column}";
        _lineColumnTextBlock.IsVisible = true;
    }

    public void SetVimMode(string mode)
    {
        _vimModeTextBlock.Text = mode;
        _vimModeTextBlock.IsVisible = mode != "Normal";
    }

    internal void UpdateLeftSidebarButtonStyle(bool isSidebarOpen) => UpdateSidebarButtonStyle(_leftSidebarToggleButton, isSidebarOpen);

    internal void UpdateRightSidebarButtonStyle(bool isSidebarOpen) => UpdateSidebarButtonStyle(_rightSidebarToggleButton, isSidebarOpen);

    private void UpdateSidebarButtonStyle(Button button, bool isSidebarOpen)
    {
        if (isSidebarOpen) button.Classes.Add("sidebar-open");
        else button.Classes.Remove("sidebar-open");
        UpdateButtonStyles(button);
    }

    public void UpdateTheme() => ApplyTheme();
}