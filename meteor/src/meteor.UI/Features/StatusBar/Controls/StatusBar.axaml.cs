using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace meteor.UI.Features.StatusBar.Controls;

public class StatusBar : UserControl
{
    private readonly IThemeManager _themeManager;
    private TextBlock _statusTextBlock;
    private TextBlock _lineColumnTextBlock;
    private Border _border;

    public event EventHandler<(int Line, int Column)> GoToLineColumnRequested;

    public StatusBar(IThemeManager themeManager)
    {
        _themeManager = themeManager;
        InitializeComponent();
        ApplyTheme();
        _themeManager.ThemeChanged += (_, _) => ApplyTheme();
    }

    private void InitializeComponent()
    {
        Height = 25;

        _statusTextBlock = new TextBlock
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 10, 0)
        };

        _lineColumnTextBlock = new TextBlock
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(10, 0, 10, 0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        _lineColumnTextBlock.PointerPressed += LineColumnTextBlock_PointerPressed;

        _border = new Border
        {
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*, Auto"),
                Children = 
                {
                    new Panel { Children = { _statusTextBlock }, [Grid.ColumnProperty] = 0 },
                    new Panel { Children = { _lineColumnTextBlock }, [Grid.ColumnProperty] = 1 }
                }
            },
            BorderThickness = new Thickness(0, 1, 0, 0)
        };

        Content = _border;
    }

    private void ApplyTheme()
    {
        var theme = _themeManager.CurrentTheme;
        Background = new SolidColorBrush(Color.Parse(theme.StatusBarColor));
        _statusTextBlock.Foreground = new SolidColorBrush(Color.Parse(theme.AppForegroundColor));
        _lineColumnTextBlock.Foreground = new SolidColorBrush(Color.Parse(theme.AppForegroundColor));
        _border.BorderBrush = new SolidColorBrush(Color.Parse(theme.BorderBrush));
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
            int.TryParse(parts[0].Split(' ')[1], out int line) &&
            int.TryParse(parts[1].Split(' ')[2], out int column))
        {
            GoToLineColumnRequested?.Invoke(this, (line, column));
        }
    }
}