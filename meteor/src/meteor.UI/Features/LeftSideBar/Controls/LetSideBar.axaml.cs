using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Services;
using meteor.UI.Features.FileExplorer.Controls;
using meteor.UI.Features.LeftSideBar.ViewModels;
using meteor.UI.Features.SourceControl.Controls;

namespace meteor.UI.Features.LeftSideBar.Controls;

public class LeftSideBar : UserControl
{
    private readonly IFileService _fileService;
    private readonly IGitService _gitService;
    private readonly IThemeManager _themeManager;
    private readonly ISearchService _searchService;
    private FileExplorerControl _fileExplorer;
    private Grid _mainGrid;
    private SearchView.Controls.SearchView _searchView;
    private StackPanel _sidebarViewSelector;
    private SourceControlView _sourceControlView;
    private readonly LeftSideBarViewModel _viewModel;
    private Button _overflowButton;
    private List<Button> _sidebarButtons;
    private ContextMenu _overflowMenu;

    public event EventHandler<string> FileSelected;
    public event EventHandler<string> DirectoryOpened;

    public LeftSideBar(IFileService fileService, IThemeManager themeManager, IGitService gitService, ISearchService searchService)
    {
        (_fileService, _themeManager, _gitService, _searchService) = (fileService, themeManager, gitService, searchService);
        InitializeComponent();
        _viewModel = new LeftSideBarViewModel(_fileService, _themeManager);
        DataContext = _viewModel;

        _viewModel.FileSelected += (_, filePath) => FileSelected?.Invoke(this, filePath);
        _viewModel.DirectoryOpened += (_, directoryPath) => DirectoryOpened?.Invoke(this, directoryPath);
        _viewModel.ViewChanged += OnViewChanged;
    }

    private void InitializeComponent()
    {
        _mainGrid = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        _sidebarViewSelector = CreateSidebarViewSelector();
        _fileExplorer = CreateFileExplorer();
        _searchView = CreateSearchView();
        _sourceControlView = CreateSourceControlView();

        _mainGrid.Children.Add(new Border
        {
            BorderThickness = new Thickness(0, 1, 0, 0),
            BorderBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BorderBrush)),
            Child = _sidebarViewSelector
        });
        Grid.SetRow(_mainGrid.Children[^1], 1);

        _mainGrid.Children.AddRange(new Control[] { _fileExplorer, _searchView, _sourceControlView });
        Content = _mainGrid;
    }

    private StackPanel CreateSidebarViewSelector()
    {
        var selector = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Height = 30,
            Background = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor))
        };

        _sidebarButtons = new List<Button>
        {
            CreateSidebarButton("\uf07c", "Files"),
            CreateSidebarButton("\uf002", "Search"),
            CreateSidebarButton("\uf126", "Source Control"),
        };

        foreach (var button in _sidebarButtons)
        {
            selector.Children.Add(button);
        }

        _overflowButton = CreateSidebarButton("\uf142", "More");
        _overflowButton.IsVisible = false;
        _overflowButton.Click += OnOverflowButtonClick;
        selector.Children.Add(_overflowButton);

        _overflowMenu = new ContextMenu();
        _overflowButton.ContextMenu = _overflowMenu;

        selector.SizeChanged += OnSelectorSizeChanged;

        return selector;
    }

    private Button CreateSidebarButton(string icon, string view)
    {
        var button = new Button
        {
            Content = new TextBlock
            {
                Text = icon,
                FontFamily = new FontFamily("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free"),
                FontSize = 16
            },
            Width = 30,
            Height = 30,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(5),
            Margin = new Thickness(2, 0, 2, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Tag = view
        };

        button.Click += (_, __) => _viewModel.SwitchViewCommand.Execute(view);
        return button;
    }

    private void OnSelectorSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        double availableWidth = _sidebarViewSelector.Bounds.Width;
        double totalWidth = 0;
        double overflowButtonWidth = _overflowButton.Width + _overflowButton.Margin.Left + _overflowButton.Margin.Right;

        _overflowButton.IsVisible = false;
        _overflowMenu.Items.Clear();

        for (int i = 0; i < _sidebarButtons.Count; i++)
        {
            var button = _sidebarButtons[i];
            double buttonWidth = button.Width + button.Margin.Left + button.Margin.Right;

            if (totalWidth + buttonWidth <= availableWidth)
            {
                button.IsVisible = true;
                totalWidth += buttonWidth;
            }
            else
            {
                // If this button doesn't fit, show the overflow button and add to menu
                _overflowButton.IsVisible = true;
                availableWidth -= overflowButtonWidth;
                button.IsVisible = false;
                AddToOverflowMenu(button);
            }
        }
    }

    private void AddToOverflowMenu(Button button)
    {
        var menuItem = new MenuItem
        {
            Header = button.Tag?.ToString(),
            Icon = new TextBlock
            {
                Text = ((TextBlock)button.Content).Text,
                FontFamily = ((TextBlock)button.Content).FontFamily,
                FontSize = ((TextBlock)button.Content).FontSize
            }
        };
        menuItem.Click += (_, __) => _viewModel.SwitchViewCommand.Execute(button.Tag?.ToString());
        _overflowMenu.Items.Add(menuItem);
    }

    private void OnOverflowButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _overflowMenu.Open(_overflowButton);
    }

    private FileExplorerControl CreateFileExplorer()
    {
        var explorer = new FileExplorerControl(_themeManager);
        explorer.FileSelected += (_, filePath) => _viewModel.FileSelectedCommand.Execute(filePath);
        explorer.DirectoryOpened += (_, directoryPath) =>
        {
            _viewModel.DirectoryOpenedCommand.Execute(directoryPath);
            _searchService.UpdateProjectRoot(directoryPath);
        };
        Grid.SetRow(explorer, 0);
        return explorer;
    }

    private SearchView.Controls.SearchView CreateSearchView()
    {
        var searchView = new SearchView.Controls.SearchView(_searchService, _themeManager);
        searchView.FileSelected += (_, args) => _viewModel.FileSelectedCommand.Execute(args.FilePath);
        Grid.SetRow(searchView, 0);
        searchView.IsVisible = false;
        return searchView;
    }

    private SourceControlView CreateSourceControlView()
    {
        var sourceControlView = new SourceControlView(_themeManager, _gitService);
        Grid.SetRow(sourceControlView, 0);
        sourceControlView.IsVisible = false;
        return sourceControlView;
    }

    public void SetDirectory(string path)
    {
        _viewModel.SetDirectoryCommand.Execute(path);
        _fileExplorer.SetDirectory(path);
        _ = _sourceControlView.UpdateChangesAsync();
        _searchView.SetSearchDirectory(path);
    }

    private void OnViewChanged(object? sender, string view)
    {
        _fileExplorer.IsVisible = view == "Files";
        _searchView.IsVisible = view == "Search";
        _sourceControlView.IsVisible = view == "Source Control";

        foreach (var child in _sidebarViewSelector.Children)
        {
            if (child is Button button)
            {
                button.Classes.Remove("selected");
                if (button.Tag?.ToString() == view) button.Classes.Add("selected");
            }
        }
    }

    internal void UpdateBackground(Core.Models.Theme theme)
    {
        _sidebarViewSelector.Background = new SolidColorBrush(Color.Parse(theme.BackgroundColor));
    }

    internal void UpdateFiles(string directoryPath)
    {
        _fileExplorer.SetDirectory(directoryPath);
    }
}