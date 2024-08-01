using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using meteor.Core.Config;
using meteor.Core.Interfaces;
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
using meteor.UI.Features.RightSideBar.Controls;
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
    private CommandPalette _commandPalette;
    private LeftSideBar _leftSideBar;
    private RightSideBar _rightSideBar;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private SourceControlView _sourceControlView;
    private StatusBar _statusBar;
    private Titlebar _titlebar;
    private GridSplitter _leftGridSplitter;
    private GridSplitter _rightGridSplitter;
    private Grid _mainGrid;
    private TabControl _tabControl;

    private readonly IEditorConfig _config;
    private readonly IScrollManager _scrollManager;
    private readonly ITabService _tabService;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IThemeManager _themeManager;
    private readonly ISearchService _searchService;
    private readonly IGitService _gitService;

    private double _leftPanelWidth = 150;
    private double _rightPanelWidth = 150;

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
        IGitService gitService,
        ISearchService searchService)
    {
        InitializeComponent();

        (_tabService, _config, _textMeasurer, _themeManager, _scrollManager, _mainWindowViewModel, _searchService, _gitService) =
            (tabService, config, textMeasurer, themeManager, scrollManager, mainWindowViewModel, searchService, gitService);

        DataContext = mainWindowViewModel;
        ClipToBounds = false;
        this.AttachDevTools();

        InitializeUI(layoutManager, inputHandler, pointerEventHandler, fileService);
        SetupEventHandlers();
    }

    private void InitializeUI(IEditorLayoutManager layoutManager, IEditorInputHandler inputHandler, IPointerEventHandler pointerEventHandler, IFileService fileService)
    {
        UpdateTheme();
        _themeManager.ThemeChanged += (_, _) => UpdateTheme();

        var editorControlFactory = new EditorControlFactory(_scrollManager, layoutManager, inputHandler,
            pointerEventHandler, _textMeasurer, _config, _themeManager);
        _tabControl = new TabControl(_tabService, editorControlFactory, _themeManager);

        _leftGridSplitter = CreateGridSplitter();
        _rightGridSplitter = CreateGridSplitter();
        _titlebar = CreateTitlebar();
        _statusBar = new StatusBar(_themeManager);
        _statusBar.LeftSidebarToggleRequested += OnLeftSidebarToggleRequested;
        _statusBar.RightSidebarToggleRequested += OnRightSidebarToggleRequested;
        _statusBar.GoToLineColumnRequested += OnGoToLineColumnRequested;

        _commandPalette = CreateCommandPalette();
        _leftSideBar = new LeftSideBar(fileService, _themeManager, _gitService, _searchService);
        _rightSideBar = new RightSideBar(_themeManager);
        _sourceControlView = new SourceControlView(_themeManager, _gitService);

        _mainGrid = CreateMainGrid();
        SetupGridLayout();

        Content = _mainGrid;
    }

    private void OnRightSidebarToggleRequested(object? sender, EventArgs e)
    {
        _mainWindowViewModel.ToggleRightSidebarCommand.Execute(null);
    }

    private void OnLeftSidebarToggleRequested(object? sender, EventArgs e)
    {
        _mainWindowViewModel.ToggleLeftSidebarCommand.Execute(null);
    }

    private GridSplitter CreateGridSplitter()
    {
        return new GridSplitter
        {
            Width = 1,
            MinWidth = 1,
            MaxWidth = 1,
            ResizeDirection = GridResizeDirection.Columns,
            Background = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BorderBrush))
        };
    }

    private Titlebar CreateTitlebar()
    {
        var titlebar = new Titlebar(_themeManager);
        titlebar.SetProjectNameFromDirectory(Environment.CurrentDirectory);
        titlebar.DirectoryOpenRequested += OnOpenDirectoryRequested;
        return titlebar;
    }

    private CommandPalette CreateCommandPalette()
    {
        var commandPalette = new CommandPalette(_themeManager)
        {
            ZIndex = 1000,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            MaxHeight = 300,
            Margin = new Thickness(0, 40, 0, 0)
        };
        commandPalette[!IsVisibleProperty] = new Binding("IsCommandPaletteVisible");
        return commandPalette;
    }

    private Grid CreateMainGrid()
    {
        return new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            ColumnDefinitions = new ColumnDefinitions($"{_leftPanelWidth},Auto,*,Auto,0"),
            ClipToBounds = false
        };
    }

    private void SetupGridLayout()
    {
        Grid.SetRow(_titlebar, 0);
        Grid.SetColumnSpan(_titlebar, 5);

        Grid.SetRow(_leftSideBar, 1);
        Grid.SetColumn(_leftSideBar, 0);

        Grid.SetRow(_leftGridSplitter, 1);
        Grid.SetColumn(_leftGridSplitter, 1);

        Grid.SetRow(_tabControl, 1);
        Grid.SetColumn(_tabControl, 2);

        Grid.SetRow(_rightGridSplitter, 1);
        Grid.SetColumn(_rightGridSplitter, 3);

        Grid.SetRow(_rightSideBar, 1);
        Grid.SetColumn(_rightSideBar, 4);

        Grid.SetRow(_statusBar, 2);
        Grid.SetColumnSpan(_statusBar, 5);

        Grid.SetRow(_commandPalette, 1);
        Grid.SetColumn(_commandPalette, 0);
        Grid.SetColumnSpan(_commandPalette, 5);

        _mainGrid.Children.AddRange([_titlebar, _leftSideBar, _leftGridSplitter, _tabControl, _rightGridSplitter, _rightSideBar, _statusBar, _commandPalette]);
    }

    private void SetupEventHandlers()
    {
        Activated += (_, _) => UpdateTitlebarBackground(true);
        Deactivated += (_, _) => UpdateTitlebarBackground(false);

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.P && e.KeyModifiers == KeyModifiers.Control)
                _mainWindowViewModel.ToggleCommandPaletteCommand.Execute(null);
        };

        PointerPressed += MainWindow_PointerPressed;

        _leftSideBar.Bind(IsVisibleProperty, new Binding("IsLeftSidebarVisible"));
        _leftGridSplitter.Bind(IsVisibleProperty, new Binding("IsLeftSidebarVisible"));
        _rightSideBar.Bind(IsVisibleProperty, new Binding("IsRightSidebarVisible"));
        _rightGridSplitter.Bind(IsVisibleProperty, new Binding("IsRightSidebarVisible"));

        _mainWindowViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.IsLeftSidebarVisible) ||
                args.PropertyName == nameof(MainWindowViewModel.IsRightSidebarVisible))
            {
                UpdateLayout();
            }
        };

        _leftSideBar.SizeChanged += (sender, args) =>
        {
            if (_mainWindowViewModel.IsLeftSidebarVisible)
            {
                _leftPanelWidth = Math.Max(args.NewSize.Width, 150);
            }
        };

        _rightSideBar.SizeChanged += (sender, args) =>
        {
            if (_mainWindowViewModel.IsRightSidebarVisible)
            {
                _rightPanelWidth = Math.Max(args.NewSize.Width, 150);
            }
        };

        _leftSideBar.FileSelected += OnFileSelected;
    }

    private void UpdateLayout()
    {
        var leftSidebarWidth = _mainWindowViewModel.IsLeftSidebarVisible ? $"{_leftPanelWidth}" : "0";
        var rightSidebarWidth = _mainWindowViewModel.IsRightSidebarVisible ? $"{_rightPanelWidth}" : "0";
        var leftSplitterWidth = _mainWindowViewModel.IsLeftSidebarVisible ? "Auto" : "0";
        var rightSplitterWidth = _mainWindowViewModel.IsRightSidebarVisible ? "Auto" : "0";

        _mainGrid.ColumnDefinitions = new ColumnDefinitions($"{leftSidebarWidth},{leftSplitterWidth},*,{rightSplitterWidth},{rightSidebarWidth}");

        Grid.SetColumn(_tabControl, 2);
        Grid.SetColumnSpan(_tabControl, 1);
    }

    private async void OnOpenDirectoryRequested(object? sender, string? e)
    {
        var storageProvider = StorageProvider ?? GetTopLevel(this)?.StorageProvider;
        if (storageProvider != null)
        {
            var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
            if (result.Count > 0)
            {
                await OnDirectoryOpenedAsync(this, result[0].Path.LocalPath);
            }
        }
    }

    private void MainWindow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_commandPalette.Bounds.Contains(e.GetPosition(this)) && _mainWindowViewModel.IsCommandPaletteVisible)
            _mainWindowViewModel.ToggleCommandPaletteCommand.Execute(null);
    }

    private void UpdateTitlebarBackground(bool isActive) => _titlebar?.UpdateBackground(isActive);

    private void UpdateTheme()
    {
        if (_themeManager?.CurrentTheme == null)
        {
            Console.WriteLine("Error: ThemeManager or CurrentTheme is null in UpdateTheme method.");
            return;
        }

        var theme = _themeManager.CurrentTheme;
        Background = new SolidColorBrush(Color.Parse(theme.AppBackgroundColor));
        UpdateTitlebarBackground(IsActive);

        if (_leftGridSplitter != null)
        {
            _leftGridSplitter.Background = new SolidColorBrush(Color.Parse(theme.BorderBrush));
        }
        if (_rightGridSplitter != null)
        {
            _rightGridSplitter.Background = new SolidColorBrush(Color.Parse(theme.BorderBrush));
        }
        _sourceControlView?.UpdateBackground(theme);
        _leftSideBar?.UpdateBackground(theme);
        _rightSideBar?.UpdateBackground(theme);
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

            var existingTab = _tabService.Tabs.FirstOrDefault(t => t?.FilePath == filePath);
            if (existingTab != null)
            {
                _tabService.SetActiveTab(existingTab);
            }
            else
            {
                CreateOrOpenTab(filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening file: {ex.Message}");
        }
    }

    private async Task OnDirectoryOpenedAsync(object? sender, string? directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            return;
        }

        _titlebar.SetProjectNameFromDirectory(directoryPath);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            await Task.Run(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await Task.WhenAll(
                        Task.Run(() => _gitService.UpdateProjectRoot(directoryPath), cts.Token),
                        Task.Run(() => _searchService.UpdateProjectRoot(directoryPath), cts.Token),
                        Task.Run(() => _leftSideBar.UpdateFiles(directoryPath), cts.Token),
                        Task.Run(() => _leftSideBar.SetDirectory(directoryPath), cts.Token),
                        _sourceControlView.UpdateChangesAsync(cts.Token)
                    );
                }, DispatcherPriority.Background);
            }, cts.Token);

            // Refresh UI components
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _leftSideBar.InvalidateVisual();
                _sourceControlView.InvalidateVisual();
            }, DispatcherPriority.Background);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Directory opening operation timed out or was cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening directory: {ex.Message}");
        }
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

    private void OnSidebarToggleRequested(object? sender, EventArgs e)
    {
        _mainWindowViewModel.ToggleLeftSidebarCommand.Execute(null);
        _statusBar.UpdateLeftSidebarButtonStyle(_mainWindowViewModel.IsLeftSidebarVisible);
    }

    private void OnGoToLineColumnRequested(object? sender, (int Line, int Column) e)
    {
        _tabService.ActiveTab?.EditorViewModel.GoToLineColumn(e.Line, e.Column);
    }
}