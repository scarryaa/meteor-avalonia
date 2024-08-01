using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

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
        _themeManager.ThemeChanged += (_, _) => ApplyTheme();
    }

    public event EventHandler<(int Line, int Column)> GoToLineColumnRequested;
    public event EventHandler LeftSidebarToggleRequested;
    public event EventHandler RightSidebarToggleRequested;

    private void InitializeComponent()
    {
        Height = 25;

        _leftSidebarToggleButton = CreateSidebarToggleButton("\uf0c9", HorizontalAlignment.Left);
        _rightSidebarToggleButton = CreateSidebarToggleButton("\uf0c9", HorizontalAlignment.Right);

        _leftSidebarToggleButton.Click += LeftSidebarToggleButton_Click;
        _rightSidebarToggleButton.Click += RightSidebarToggleButton_Click;

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

        _lineColumnTextBlock.PointerPressed += LineColumnTextBlock_PointerPressed;

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
            Margin = new Thickness(0, 0, 0, 0),
            Classes = { "sidebar-toggle", "sidebar-open" }
        };

        button.Styles.Add(new Style(x => x.OfType<Button>().Class("sidebar-toggle"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
            }
        });

        button.Styles.Add(new Style(x => x.OfType<Button>().Class("sidebar-toggle").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.Parse("#3A3A3A"))),
            }
        });

        button.Styles.Add(new Style(x => x.OfType<Button>().Class("sidebar-toggle").Class(":pressed"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.Parse("#1E1E1E"))),
            }
        });

        button.Styles.Add(new Style(x => x.OfType<Button>().Class("sidebar-toggle").Class("sidebar-open"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.Parse("#1E1E1E"))),
            }
        });

        return button;
    }

    private void ApplyTheme()
    {
        var theme = _themeManager.CurrentTheme;
        Background = new SolidColorBrush(Color.Parse(theme.StatusBarColor));
        _statusTextBlock.Foreground = new SolidColorBrush(Color.Parse(theme.AppForegroundColor));
        _lineColumnTextBlock.Foreground = new SolidColorBrush(Color.Parse(theme.AppForegroundColor));
        _border.BorderBrush = new SolidColorBrush(Color.Parse(theme.BorderBrush));
        _leftSidebarToggleButton.Foreground = new SolidColorBrush(Color.Parse(theme.AppForegroundColor));
        _rightSidebarToggleButton.Foreground = new SolidColorBrush(Color.Parse(theme.AppForegroundColor));
    }

    public void SetStatus(string status)
    {
        _statusTextBlock.Text = status;
    }

    public void SetLineAndColumn(int line, int column)
    {
        _lineColumnTextBlock.Text = $"Ln {line}, Col {column}";
    }

    private void LineColumnTextBlock_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        var parts = _lineColumnTextBlock.Text.Split(',');
        if (parts.Length == 2 &&
            int.TryParse(parts[0].Split(' ')[1], out var line) &&
            int.TryParse(parts[1].Split(' ')[2], out var column))
            GoToLineColumnRequested?.Invoke(this, (line, column));
    }

    private void LeftSidebarToggleButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        LeftSidebarToggleRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RightSidebarToggleButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        RightSidebarToggleRequested?.Invoke(this, EventArgs.Empty);
    }

    internal void UpdateLeftSidebarButtonStyle(bool isSidebarOpen)
    {
        UpdateSidebarButtonStyle(_leftSidebarToggleButton, isSidebarOpen);
    }

    internal void UpdateRightSidebarButtonStyle(bool isSidebarOpen)
    {
        UpdateSidebarButtonStyle(_rightSidebarToggleButton, isSidebarOpen);
    }

    private void UpdateSidebarButtonStyle(Button button, bool isSidebarOpen)
    {
        if (isSidebarOpen)
        {
            button.Classes.Add("sidebar-open");
        }
        else
        {
            button.Classes.Remove("sidebar-open");
        }
    }
}