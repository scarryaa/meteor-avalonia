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
    private TextBlock _statusTextBlock;
    private Button _leftSidebarToggleButton;
    private Button _rightSidebarToggleButton;

    public StatusBar(IThemeManager themeManager)
    {
        _themeManager = themeManager;
        InitializeComponent();
        ApplyTheme();
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    public event EventHandler<(int Line, int Column)> GoToLineColumnRequested;
    public event EventHandler LeftSidebarToggleRequested;
    public event EventHandler RightSidebarToggleRequested;

    private void InitializeComponent()
    {
        Height = 25;

        _leftSidebarToggleButton = CreateSidebarToggleButton("\uf0c9", HorizontalAlignment.Left);
        _rightSidebarToggleButton = CreateSidebarToggleButton("\uf0c9", HorizontalAlignment.Right);

        _leftSidebarToggleButton.Click += (_, _) => LeftSidebarToggleRequested?.Invoke(this, EventArgs.Empty);
        _rightSidebarToggleButton.Click += (_, _) => RightSidebarToggleRequested?.Invoke(this, EventArgs.Empty);

        _statusTextBlock = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 10, 0)
        };

        _lineColumnTextBlock = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10, 0, 10, 0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        _lineColumnTextBlock.PointerPressed += (_, _) =>
        {
            var parts = _lineColumnTextBlock.Text.Split(',');
            if (parts.Length == 2 &&
                int.TryParse(parts[0].Split(' ')[1], out var line) &&
                int.TryParse(parts[1].Split(' ')[2], out var column))
                GoToLineColumnRequested?.Invoke(this, (line, column));
        };

        _border = new Border
        {
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
                Children =
                {
                    new Panel { Children = { _leftSidebarToggleButton }, [Grid.ColumnProperty] = 0 },
                    new Panel { Children = { _statusTextBlock }, [Grid.ColumnProperty] = 1 },
                    new Panel { Children = { _lineColumnTextBlock }, [Grid.ColumnProperty] = 2 },
                    new Panel { Children = { _rightSidebarToggleButton }, [Grid.ColumnProperty] = 3 }
                }
            },
            BorderThickness = new Thickness(0, 1, 0, 0)
        };

        Content = _border;
    }

    private Button CreateSidebarToggleButton(string icon, HorizontalAlignment alignment)
    {
        var button = new Button
        {
            Content = new TextBlock
            {
                Text = icon,
                FontFamily = new FontFamily("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free"),
                FontSize = 13,
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

        UpdateButtonStyles(button);

        return button;
    }

    private void UpdateButtonStyles(Button button)
    {
        var theme = _themeManager.CurrentTheme;

        button.Styles.Add(new Style(x => x.OfType<Button>().Class("sidebar-toggle"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
            }
        });

        if (Color.TryParse(theme.ButtonHoverColor, out var hoverColor))
        {
            button.Styles.Add(new Style(x => x.OfType<Button>().Class("sidebar-toggle").Class(":pointerover"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, new SolidColorBrush(hoverColor)),
                }
            });
        }

        if (Color.TryParse(theme.ButtonPressedColor, out var pressedColor))
        {
            button.Styles.Add(new Style(x => x.OfType<Button>().Class("sidebar-toggle").Class(":pressed"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, new SolidColorBrush(pressedColor)),
                }
            });
        }

        if (Color.TryParse(theme.ButtonActiveColor, out var activeColor))
        {
            button.Styles.Add(new Style(x => x.OfType<Button>().Class("sidebar-toggle").Class("sidebar-open"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, new SolidColorBrush(activeColor)),
                }
            });
        }
    }

    private void ApplyTheme()
    {
        Background = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.StatusBarColor));
        _statusTextBlock.Foreground = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.AppForegroundColor));
        _lineColumnTextBlock.Foreground = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.AppForegroundColor));
        _border.BorderBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BorderBrush));
        _leftSidebarToggleButton.Foreground = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.AppForegroundColor));
        _rightSidebarToggleButton.Foreground = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.AppForegroundColor));

        UpdateButtonStyles(_leftSidebarToggleButton);
        UpdateButtonStyles(_rightSidebarToggleButton);
    }

    private void OnThemeChanged(object sender, Theme theme)
    {
        ApplyTheme();
        InvalidateVisual();
    }

    public void SetStatus(string status) => _statusTextBlock.Text = status;

    public void SetLineAndColumn(int line, int column) => _lineColumnTextBlock.Text = $"Ln {line}, Col {column}";

    internal void UpdateLeftSidebarButtonStyle(bool isSidebarOpen) => UpdateSidebarButtonStyle(_leftSidebarToggleButton, isSidebarOpen);

    internal void UpdateRightSidebarButtonStyle(bool isSidebarOpen) => UpdateSidebarButtonStyle(_rightSidebarToggleButton, isSidebarOpen);

    private void UpdateSidebarButtonStyle(Button button, bool isSidebarOpen)
    {
        if (isSidebarOpen) button.Classes.Add("sidebar-open");
        else button.Classes.Remove("sidebar-open");
        UpdateButtonStyles(button);
    }

    public void UpdateTheme()
    {
        ApplyTheme();
        InvalidateVisual();
    }
}