using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using meteor.Core.Config;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Models;
using meteor.Core.Services;
using meteor.UI.Features.CommandPalette.Controls;
using meteor.UI.Features.Editor.Factories;
using meteor.UI.Features.Editor.Interfaces;
using meteor.UI.Features.Editor.ViewModels;
using meteor.UI.Features.LeftSideBar.Controls;
using meteor.UI.Features.SourceControl.Controls;
using meteor.UI.Features.StatusBar.Controls;
using meteor.UI.Features.Tabs.ViewModels;
using meteor.UI.Features.Titlebar.Controls;
using meteor.UI.Services;
using meteor.UI.ViewModels;
using Color = Avalonia.Media.Color;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using TabControl = meteor.UI.Features.Tabs.Controls.TabControl;

namespace meteor.UI.Views;

public partial class MainWindow : Window
{
    private readonly CommandPalette _commandPalette;
    private readonly IEditorConfig _config;
    private readonly LeftSideBar _leftSideBar;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IScrollManager _scrollManager;
    private readonly SourceControlView _sourceControlView;
    private readonly StatusBar _statusBar;
    private readonly ITabService _tabService;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IThemeManager _themeManager;
    private readonly Titlebar _titlebar;
    private readonly GridSplitter _gridSplitter;

    public MainWindow(
        MainWindowViewModel mainWindowViewModel,
        ITabService tabService,
        IEditorLayoutManager layoutManager,
        IEditorInputHandler inputHandler,
        ITextMeasurer textMeasurer,
        IEditorConfig config,
        IScrollManager scrollManager,
        IPointerEventHandler pointerEventHandler,
        IThemeManager themeManager,
        IFileService fileService,
        IGitService gitService)
    {
        InitializeComponent();

        _tabService = tabService;
        _config = config;
        _textMeasurer = textMeasurer;
        _themeManager = themeManager;
        _scrollManager = scrollManager;
        _mainWindowViewModel = mainWindowViewModel;

        DataContext = mainWindowViewModel;
        ClipToBounds = false;
        this.AttachDevTools();

        UpdateTheme();
        _themeManager.ThemeChanged += (_, _) => UpdateTheme();

        var editorControlFactory = new EditorControlFactory(scrollManager, layoutManager, inputHandler,
            pointerEventHandler, _textMeasurer, _config, themeManager);
        var tabControl = new TabControl(tabService, editorControlFactory, themeManager);

        _gridSplitter = new GridSplitter
        {
            Width = 1,
            MinWidth = 1,
            MaxWidth = 1,
            ResizeDirection = GridResizeDirection.Columns,
            Background = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BorderBrush))
        };

        _titlebar = new Titlebar(_themeManager);
        _titlebar.SetProjectNameFromDirectory(Environment.CurrentDirectory);
        _titlebar.DirectoryOpenRequested += OnDirectoryOpenRequested;

        _statusBar = new StatusBar(_themeManager);

        _commandPalette = new CommandPalette(_themeManager);
        _commandPalette.ZIndex = 1000;
        _commandPalette.HorizontalAlignment = HorizontalAlignment.Center;
        _commandPalette.VerticalAlignment = VerticalAlignment.Top;
        _commandPalette.Width = 400;
        _commandPalette.MaxHeight = 300;
        _commandPalette.Margin = new Thickness(0, 40, 0, 0);
        _commandPalette[!IsVisibleProperty] = new Binding("IsCommandPaletteVisible");

        _leftSideBar = new LeftSideBar(fileService, _themeManager, gitService);
        _leftSideBar.FileSelected += OnFileSelected;
        _leftSideBar.DirectoryOpened += OnDirectoryOpened;
        _sourceControlView = new SourceControlView(_themeManager, gitService);

        var mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            ColumnDefinitions = new ColumnDefinitions("150,Auto,*")
        };

        Grid.SetRow(_titlebar, 0);
        Grid.SetColumnSpan(_titlebar, 3);

        Grid.SetRow(_leftSideBar, 1);
        Grid.SetColumn(_leftSideBar, 0);

        Grid.SetRow(_gridSplitter, 1);
        Grid.SetColumn(_gridSplitter, 1);

        Grid.SetRow(tabControl, 1);
        Grid.SetColumn(tabControl, 2);

        Grid.SetRow(_statusBar, 2);
        Grid.SetColumnSpan(_statusBar, 3);

        Grid.SetRow(_commandPalette, 1);
        Grid.SetColumn(_commandPalette, 0);
        Grid.SetColumnSpan(_commandPalette, 3);

        mainGrid.Children.Add(_titlebar);
        mainGrid.Children.Add(_leftSideBar);
        mainGrid.Children.Add(_gridSplitter);
        mainGrid.Children.Add(tabControl);
        mainGrid.Children.Add(_statusBar);
        mainGrid.Children.Add(_commandPalette);

        Content = mainGrid;
        mainGrid.ClipToBounds = false;

        Activated += (_, _) => UpdateTitlebarBackground(true);
        Deactivated += (_, _) => UpdateTitlebarBackground(false);

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.P && e.KeyModifiers == KeyModifiers.Control)
                mainWindowViewModel.ToggleCommandPaletteCommand.Execute(null);
        };

        PointerPressed += MainWindow_PointerPressed;
    }

    private void MainWindow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        if (!_commandPalette.Bounds.Contains(point) && _mainWindowViewModel.IsCommandPaletteVisible)
            _mainWindowViewModel.ToggleCommandPaletteCommand.Execute(null);
    }

    private void UpdateTitlebarBackground(bool isActive)
    {
        if (_titlebar != null)
            _titlebar.UpdateBackground(isActive);
    }

    private async void OnDirectoryOpenRequested(object? sender, string e)
    {
        var storageProvider = StorageProvider ?? GetTopLevel(this)?.StorageProvider;
        if (storageProvider != null)
        {
            var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
            if (result.Count > 0)
            {
                var selectedFolder = result[0];
                OnDirectoryOpened(this, selectedFolder.Path.LocalPath);
            }
        }
    }

    private void UpdateTheme()
    {
        if (_themeManager == null || _themeManager.CurrentTheme == null)
        {
            Console.WriteLine("Error: ThemeManager or CurrentTheme is null in UpdateTheme method.");
            return;
        }

        var theme = _themeManager.CurrentTheme;
        Background = new SolidColorBrush(Color.Parse(theme.AppBackgroundColor));
        UpdateTitlebarBackground(IsActive);

        // Update GridSplitter background
        if (_gridSplitter != null) _gridSplitter.Background = new SolidColorBrush(Color.Parse(theme.BorderBrush));
    }

    private void OnFileSelected(object? sender, string? filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                CreateOrOpenTab();
                return;
            }

            // Check if the file is already open
            var existingTab = _tabService.Tabs.FirstOrDefault(t => t?.FilePath == filePath);
            if (existingTab != null)
            {
                _tabService.SetActiveTab(existingTab);
                return;
            }

            // Open the file in a new tab
            CreateOrOpenTab(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening file: {ex.Message}");
        }
    }

    private void OnDirectoryOpened(object? sender, string? directoryPath)
    {
        _titlebar.SetProjectNameFromDirectory(directoryPath);
        _sourceControlView.UpdateChangesAsync();
    }

    private void CreateOrOpenTab(string? filePath = null)
    {
        var editorViewModel = CreateEditorViewModel();
        var tabConfig = new TabConfig(_tabService, _themeManager);

        if (string.IsNullOrEmpty(filePath))
        {
            _tabService.AddTab(editorViewModel, tabConfig, "Untitled", string.Empty);
        }
        else
        {
            var fileContent = File.ReadAllText(filePath, Encoding.UTF8);
            var fileName = Path.GetFileName(filePath);
            var newTab = _tabService.AddTab(editorViewModel, tabConfig, fileName, filePath, fileContent);

            if (newTab is TabViewModel tabViewModel) tabViewModel.SetFilePath(filePath);
        }
    }

    private EditorViewModel CreateEditorViewModel()
    {
        var textBufferService = new TextBufferService(new TextBuffer(), _textMeasurer, _config);
        var editorConfig = new EditorConfig();
        var cursorManager = new CursorManager(textBufferService, editorConfig);
        var clipboardManager = new ClipboardManager { TopLevelRef = this };
        var selectionManager = new SelectionManager(textBufferService);
        var textAnalysisService = new TextAnalysisService();
        var inputManager = new InputManager(textBufferService, cursorManager, clipboardManager, selectionManager,
            textAnalysisService, _scrollManager);

        var editorViewModel = new EditorViewModel(
            textBufferService,
            cursorManager,
            inputManager,
            selectionManager,
            editorConfig,
            _textMeasurer,
            new CompletionProvider(textBufferService)
        );
        inputManager.SetViewModel(editorViewModel);

        return editorViewModel;
    }
}