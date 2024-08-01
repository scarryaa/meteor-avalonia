using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;

namespace meteor.UI.Features.Titlebar.Controls;

public class Titlebar : UserControl
{
    private const int TitlebarHeight = 30;

    public static readonly DirectProperty<Titlebar, string> ProjectNameProperty =
        AvaloniaProperty.RegisterDirect<Titlebar, string>(
            nameof(ProjectName),
            o => o.ProjectName,
            (o, v) => o.ProjectName = v);

    private readonly IThemeManager _themeManager;
    private Window _parentWindow;

    private string _projectName;
    private Button _projectNameButton;

    public Titlebar(IThemeManager themeManager)
    {
        _themeManager = themeManager;
        InitializeComponent();
        AdjustLayoutForOS();
        Height = TitlebarHeight;
        ApplyTheme();
        _themeManager.ThemeChanged += (_, _) => ApplyTheme();

        PointerPressed += OnPointerPressed;
        DoubleTapped += OnDoubleTapped;
    }

    public string ProjectName
    {
        get => _projectName;
        set
        {
            SetAndRaise(ProjectNameProperty, ref _projectName, value);
            if (_projectNameButton != null) _projectNameButton.Content = value;
        }
    }

    public event EventHandler<string> DirectoryOpenRequested;

    private void InitializeComponent()
    {
        _projectNameButton = new Button
        {
            Name = "ProjectNameButton",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(10, 0, 0, 0),
            Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(5, 3),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        _projectNameButton.Click += ProjectNameButton_Click;

        Content = _projectNameButton;
    }

    private void ProjectNameButton_Click(object? sender, RoutedEventArgs e)
    {
        DirectoryOpenRequested?.Invoke(this, null);
    }

    private void AdjustLayoutForOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            // On macOS, we need to account for the traffic lights
            Padding = new Thickness(60, 0, 0, 0);
        else
            // On Windows/Linux, we need to account for the window buttons on the right
            Padding = new Thickness(0, 0, 140, 0);
    }

    public void UpdateBackground(bool isActive)
    {
        var theme = _themeManager.CurrentTheme;
        var color = isActive ? theme.TitleBarColor : theme.TitleBarInactiveColor;
        Background = new SolidColorBrush(Color.Parse(color));
    }

    private void ApplyTheme()
    {
        var theme = _themeManager.CurrentTheme;
        if (_parentWindow != null && _parentWindow.IsActive)
            Background = new SolidColorBrush(Color.Parse(theme.TitleBarColor));
        else
            Background = new SolidColorBrush(Color.Parse(theme.TitleBarInactiveColor));
    }

    public void SetProjectNameFromDirectory(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
            ProjectName = "Untitled Project";
        else
            ProjectName = Path.GetFileName(directoryPath);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (this.GetVisualRoot() is Window window) window.BeginMoveDrag(e);
        }
        else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed &&
                 !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            OpenContextMenu();
            e.Handled = true;
        }
    }

    private void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (this.GetVisualRoot() is Window window)
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }

    private void OpenContextMenu()
    {
        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(new MenuItem
            { Header = "Minimize", Command = new RelayCommand(() => ToggleWindowState(WindowState.Minimized)) });
        contextMenu.Items.Add(new MenuItem
            { Header = GetMaximizeRestoreMenuItemHeader(), Command = new RelayCommand(() => ToggleMaximizeRestore()) });
        contextMenu.Items.Add(new MenuItem { Header = "Close", Command = new RelayCommand(() => CloseWindow()) });
        contextMenu.Open(this);
    }

    private void ToggleWindowState(WindowState state)
    {
        if (this.GetVisualRoot() is Window window)
            window.WindowState = window.WindowState == state ? WindowState.Normal : state;
    }

    private void ToggleMaximizeRestore()
    {
        if (this.GetVisualRoot() is Window window)
            window.WindowState =
                window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private string GetMaximizeRestoreMenuItemHeader()
    {
        if (this.GetVisualRoot() is Window window)
            return window.WindowState == WindowState.Maximized ? "Restore" : "Maximize";
        return "Maximize";
    }

    private void CloseWindow()
    {
        if (this.GetVisualRoot() is Window window) window.Close();
    }
}