using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
    private FileExplorerControl _fileExplorer;
    private Grid _mainGrid;
    private SearchView.Controls.SearchView _searchView;
    private Grid _sidebarViewSelector;
    private SourceControlView _sourceControlView;
    private readonly LeftSideBarViewModel _viewModel;

    public LeftSideBar(IFileService fileService, IThemeManager themeManager, IGitService gitService)
    {
        _fileService = fileService;
        _themeManager = themeManager;
        _gitService = gitService;
        InitializeComponent();
        _viewModel = new LeftSideBarViewModel(_fileService, _themeManager);
        DataContext = _viewModel;

        _viewModel.FileSelected += (_, filePath) => FileSelected?.Invoke(this, filePath);
        _viewModel.DirectoryOpened += (_, directoryPath) => DirectoryOpened?.Invoke(this, directoryPath);
        _viewModel.ViewChanged += OnViewChanged;
    }

    public event EventHandler<string> FileSelected;
    public event EventHandler<string> DirectoryOpened;

    private void InitializeComponent()
    {
        _mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto")
        };

        _sidebarViewSelector = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            Height = 30,
            Background = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor))
        };

        var fileExplorerButton = CreateSidebarButton("\uf07c", "Files"); // Font Awesome folder icon
        var searchButton = CreateSidebarButton("\uf002", "Search"); // Font Awesome search icon
        var sourceControlButton = CreateSidebarButton("\uf126", "Source Control"); // Font Awesome code-branch icon

        Grid.SetColumn(fileExplorerButton, 0);
        Grid.SetColumn(searchButton, 1);
        Grid.SetColumn(sourceControlButton, 2);

        _sidebarViewSelector.Children.Add(fileExplorerButton);
        _sidebarViewSelector.Children.Add(searchButton);
        _sidebarViewSelector.Children.Add(sourceControlButton);

        var border = new Border
        {
            BorderThickness = new Thickness(0, 1, 0, 0),
            BorderBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BorderBrush)),
            Child = _sidebarViewSelector
        };

        Grid.SetRow(border, 1);
        _mainGrid.Children.Add(border);

        _fileExplorer = new FileExplorerControl(_themeManager);
        _fileExplorer.FileSelected += (_, filePath) => _viewModel.FileSelectedCommand.Execute(filePath);
        _fileExplorer.DirectoryOpened += (_, directoryPath) => _viewModel.DirectoryOpenedCommand.Execute(directoryPath);
        Grid.SetRow(_fileExplorer, 0);
        _mainGrid.Children.Add(_fileExplorer);

        _searchView = new SearchView.Controls.SearchView(_themeManager, _fileService);
        _searchView.FileSelected += (_, filePath) => _viewModel.FileSelectedCommand.Execute(filePath);
        Grid.SetRow(_searchView, 0);
        _mainGrid.Children.Add(_searchView);
        _searchView.IsVisible = false;

        _sourceControlView = new SourceControlView(_themeManager, _gitService);
        Grid.SetRow(_sourceControlView, 0);
        _mainGrid.Children.Add(_sourceControlView);
        _sourceControlView.IsVisible = false;

        Content = _mainGrid;
    }

    private Button CreateSidebarButton(string icon, string view)
    {
        var button = new Button
        {
            Content = new TextBlock
            {
                Text = icon,
                FontFamily =
                    new FontFamily(
                        "avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free"),
                FontSize = 16
            },
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
            Tag = view
        };

        button.Click += (_, __) => _viewModel.SwitchViewCommand.Execute(view);

        return button;
    }

    public void SetDirectory(string path)
    {
        _viewModel.SetDirectoryCommand.Execute(path);
        _fileExplorer.SetDirectory(path);
        _sourceControlView.UpdateChangesAsync();
    }

    private void OnViewChanged(object sender, string view)
    {
        _fileExplorer.IsVisible = false;
        _searchView.IsVisible = false;
        _sourceControlView.IsVisible = false;

        switch (view)
        {
            case "Files":
                _fileExplorer.IsVisible = true;
                break;
            case "Search":
                _searchView.IsVisible = true;
                break;
            case "Source Control":
                _sourceControlView.IsVisible = true;
                break;
        }

        foreach (var child in _sidebarViewSelector.Children)
            if (child is Button button)
            {
                button.Classes.Remove("selected");
                if (button.Tag?.ToString() == view) button.Classes.Add("selected");
            }
    }

    internal void UpdateBackground(Core.Models.Theme theme)
    {
        _sidebarViewSelector.Background = new SolidColorBrush(Color.Parse(theme.BackgroundColor));
    }
}