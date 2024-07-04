using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using meteor.Enums;
using meteor.Interfaces;
using meteor.Models;
using meteor.Views.Services;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using File = System.IO.File;

namespace meteor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private Stack<TabViewModel> _tabSelectionHistory;
    private TabViewModel? _selectedTab;
    private double _windowWidth;
    private double _windowHeight;
    private bool _isDialogOpen;
    private string _selectedPath;
    private bool _suppressHistoryTracking;
    private TabViewModel _temporaryTab;
    private readonly ITextBufferFactory _textBufferFactory;
    private IAutoSaveService _autoSaveService;
    private bool _isCommandPaletteVisible;

    public IResourceProvider CurrentTheme => ThemeService.GetCurrentTheme();
    public CommandPaletteViewModel CommandPaletteViewModel { get; }
    
    public MainWindowViewModel(
        TitleBarViewModel titleBarViewModel,
        StatusPaneViewModel statusPaneViewModel,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel,
        ICursorPositionService cursorPositionService,
        FileExplorerViewModel fileExplorerViewModel,
        ITextBufferFactory textBufferFactory,
        IAutoSaveService autoSaveService,
        IDialogService dialogService,
        IThemeService themeService)
    {
        NewTabCommand = ReactiveCommand.Create(NewTab);
        CloseTabCommand = ReactiveCommand.Create<TabViewModel>(async tab => await CloseTabAsync(tab));
        SaveCommand = ReactiveCommand.Create(SaveCurrentFile);
        OpenFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
        ShowSaveDialogCommand = ReactiveCommand.Create(() => IsDialogOpen = true);
        HideSaveDialogCommand = ReactiveCommand.Create(() => IsDialogOpen = false);
        SetLightThemeCommand = ReactiveCommand.Create(SetLightTheme);
        SetDarkThemeCommand = ReactiveCommand.Create(SetDarkTheme);
        ToggleCommandPaletteCommand = ReactiveCommand.Create(ToggleCommandPalette);
        
        ThemeService = themeService;
        _autoSaveService = autoSaveService;
        _dialogService = dialogService;
        
        _textBufferFactory = textBufferFactory;
        StatusPaneViewModel = statusPaneViewModel;
        FileExplorerViewModel = fileExplorerViewModel;
        TitleBarViewModel = titleBarViewModel;
        CommandPaletteViewModel = new CommandPaletteViewModel(this);
        _isCommandPaletteVisible = false;

        Tabs = new ObservableCollection<TabViewModel>();
        
        _tabSelectionHistory = new Stack<TabViewModel>();

        SelectedTab = Tabs.FirstOrDefault();

        this.WhenAnyValue(x => x.SelectedTab)
            .Subscribe(tab =>
            {
                if (tab != null)
                {
                    var scrollableTextEditorVm = tab.ScrollableTextEditorViewModel;
                    scrollableTextEditorVm.UpdateViewProperties();
                    scrollableTextEditorVm.WindowHeight = WindowHeight;
                    scrollableTextEditorVm.WindowWidth = WindowWidth;

                    scrollableTextEditorVm.TextEditorViewModel.TextBuffer =
                        tab.ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer;

                    scrollableTextEditorVm.TextEditorViewModel.OnInvalidateRequired();
                    cursorPositionService.UpdateCursorPosition(
                        scrollableTextEditorVm.TextEditorViewModel.CursorPosition,
                        scrollableTextEditorVm.TextEditorViewModel.TextBuffer.LineStarts);
                }
            });

        this.WhenAnyValue(x => x.WindowHeight)
            .Subscribe(height =>
            {
                foreach (var tab in Tabs) tab.ScrollableTextEditorViewModel.WindowHeight = height;
            });

        this.WhenAnyValue(x => x.WindowWidth)
            .Subscribe(width =>
            {
                foreach (var tab in Tabs) tab.ScrollableTextEditorViewModel.WindowWidth = width;
            });

        this.WhenAnyValue(x => x.FileExplorerViewModel.SelectedPath)
            .Subscribe(path =>
            {
                SelectedPath = path;
                if (SelectedPath != string.Empty && SelectedPath != null)
                    titleBarViewModel.OpenProjectName = SelectedPath.Split('/').Last();
            });
    }

    public string SelectedPath
    {
        get => _selectedPath;
        set => this.RaiseAndSetIfChanged(ref _selectedPath, value);
    }

    public bool IsCommandPaletteVisible
    {
        get => _isCommandPaletteVisible;
        set
        {
            this.RaiseAndSetIfChanged(ref _isCommandPaletteVisible, value);
            CommandPaletteViewModel.IsVisible = value;
        }
    }

    public IThemeService ThemeService { get; }

    public TabViewModel SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab != null)
            {
                _selectedTab.SavedVerticalOffset = _selectedTab.ScrollableTextEditorViewModel.VerticalOffset;
                _selectedTab.SavedHorizontalOffset = _selectedTab.ScrollableTextEditorViewModel.HorizontalOffset;
                _selectedTab.IsSelected = false;
            }

            this.RaiseAndSetIfChanged(ref _selectedTab, value);

            if (_selectedTab != null)
            {
                _selectedTab.IsSelected = true;
                Dispatcher.UIThread.Post(() =>
                {
                    _selectedTab.ScrollableTextEditorViewModel.TextEditorViewModel.Focus();
                    _selectedTab.ScrollableTextEditorViewModel.VerticalOffset = _selectedTab.SavedVerticalOffset;
                    _selectedTab.ScrollableTextEditorViewModel.HorizontalOffset = _selectedTab.SavedHorizontalOffset;
                    
                    _selectedTab.ScrollableTextEditorViewModel.UpdateLongestLineWidth();
                }, DispatcherPriority.Render);
            }
        }
    }

    public ObservableCollection<TabViewModel> Tabs { get; }
    public StatusPaneViewModel StatusPaneViewModel { get; }
    public FileExplorerViewModel FileExplorerViewModel { get; }
    public TitleBarViewModel TitleBarViewModel { get; }

    public double WindowWidth
    {
        get => _windowWidth;
        set => this.RaiseAndSetIfChanged(ref _windowWidth, value);
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set => this.RaiseAndSetIfChanged(ref _windowHeight, value);
    }

    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set => this.RaiseAndSetIfChanged(ref _isDialogOpen, value);
    }

    public ICommand NewTabCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleCommandPaletteCommand { get; }
    public ReactiveCommand<Unit, Unit> SetLightThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> SetDarkThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, bool> ShowSaveDialogCommand { get; }
    public ReactiveCommand<Unit, bool> HideSaveDialogCommand { get; }

    private void ToggleCommandPalette()
    {
        IsCommandPaletteVisible = !IsCommandPaletteVisible;
    }
    
    private void SetLightTheme()
    {
        ThemeService.SetTheme("avares://meteor/Resources/LightTheme.axaml");
    }

    private void SetDarkTheme()
    {
        ThemeService.SetTheme("avares://meteor/Resources/DarkTheme.axaml");
    }
    
    private async Task OpenFolderAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Folder"
        };

        var window = Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (window != null)
        {
            var result = await dialog.ShowAsync(window);
            if (result != null)
            {
                SelectedPath = result;
                FileExplorerViewModel.SelectedPath = result; // Update FileExplorerViewModel
            }
        }
    }

    private void NewTab()
    {
        var cursorPositionService = App.ServiceProvider.GetRequiredService<ICursorPositionService>();
        var undoRedoManager = App.ServiceProvider.GetRequiredService<IUndoRedoManager<TextState>>();
        var fileSystemWatcherFactory = App.ServiceProvider.GetRequiredService<IFileSystemWatcherFactory>();
        var textBuffer = _textBufferFactory.Create();
        var fontPropertiesViewModel = App.ServiceProvider.GetRequiredService<FontPropertiesViewModel>();
        var lineCountViewModel = App.ServiceProvider.GetRequiredService<LineCountViewModel>();
        var clipboardService = App.ServiceProvider.GetRequiredService<IClipboardService>();
        var autoSaveService = App.ServiceProvider.GetRequiredService<IAutoSaveService>();
        var themeService = App.ServiceProvider.GetRequiredService<IThemeService>();

        var textEditorViewModel = new TextEditorViewModel(
            cursorPositionService,
            fontPropertiesViewModel,
            lineCountViewModel,
            textBuffer,
            clipboardService,
            this);

        var scrollManager = new ScrollManager(textEditorViewModel);
        var scrollableTextEditorViewModel = new ScrollableTextEditorViewModel(
            cursorPositionService,
            fontPropertiesViewModel,
            lineCountViewModel,
            textBuffer,
            clipboardService,
            themeService,
            textEditorViewModel,
            scrollManager);

        var newTab = new TabViewModel(
            cursorPositionService,
            undoRedoManager,
            fileSystemWatcherFactory,
            _textBufferFactory,
            fontPropertiesViewModel,
            lineCountViewModel,
            clipboardService,
            autoSaveService,
            themeService)
        {
            Title = $"Untitled {Tabs.Count + 1}",
            ScrollableTextEditorViewModel = scrollableTextEditorViewModel
        };

        Tabs.Add(newTab);
        SelectedTab = newTab;

        if (SelectedTab != null)
            SelectedTab.ScrollableTextEditorViewModel.Viewport = new Size(WindowWidth, WindowHeight);
    }

    private async Task CloseTabAsync(TabViewModel tab)
    {
        if (tab == null || !Tabs.Contains(tab))
            return;

        try
        {
            if (tab.IsDirty)
            {
                var saveResult = await ShowSaveConfirmationDialogAsync(tab);
                switch (saveResult)
                {
                    case SaveConfirmationResult.Save:
                        await tab.SaveAsync();
                        break;
                    case SaveConfirmationResult.DontSave:
                        // Proceed without saving
                        break;
                    case SaveConfirmationResult.Cancel:
                        return; // Exit without closing the tab
                }
            }

            // Cleanup auto-save backups
            await tab.CleanupBackupsAsync();

            Tabs.Remove(tab);

            if (tab == _temporaryTab)
                _temporaryTab = null;

            UpdateTabSelectionAfterClose(tab);
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error closing tab: {ex.Message}");
            await ShowErrorDialogAsync("Failed to close the tab. Please try again.");
        }
    }

    private void UpdateTabSelectionAfterClose(TabViewModel closedTab)
    {
        if (closedTab != SelectedTab)
            return;

        // Remove the closed tab from the selection history
        _tabSelectionHistory = new Stack<TabViewModel>(
            _tabSelectionHistory.Where(t => t != closedTab && Tabs.Contains(t)));

        // Temporarily suppress history tracking for auto-selection
        _suppressHistoryTracking = true;

        // Set the last selected tab if available
        if (_tabSelectionHistory.Count > 0)
            SelectedTab = _tabSelectionHistory.Pop();
        else if (Tabs.Count > 0)
            SelectedTab = Tabs[0];
        else
            SelectedTab = null;

        _suppressHistoryTracking = false;
    }

    private async Task<SaveConfirmationResult> ShowSaveConfirmationDialogAsync(TabViewModel tab)
    {
        var result = await _dialogService.ShowContentDialogAsync(
            this, // Pass the MainWindowViewModel instance
            "Save Changes",
            $"Do you want to save changes to {tab.Title}?",
            "Save",
            "Don't Save",
            "Cancel"
        );

        return result switch
        {
            ContentDialogResult.Primary => SaveConfirmationResult.Save,
            ContentDialogResult.Secondary => SaveConfirmationResult.DontSave,
            _ => SaveConfirmationResult.Cancel
        };
    }

    public void OnFileClicked(string filePath)
    {
        var existingTab = Tabs.FirstOrDefault(tab => tab.Title == Path.GetFileName(filePath));
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        if (_temporaryTab == null)
        {
            _temporaryTab = new TabViewModel(
                App.ServiceProvider.GetRequiredService<ICursorPositionService>(),
                App.ServiceProvider.GetRequiredService<IUndoRedoManager<TextState>>(),
                App.ServiceProvider.GetRequiredService<IFileSystemWatcherFactory>(),
                _textBufferFactory,
                App.ServiceProvider.GetRequiredService<FontPropertiesViewModel>(),
                App.ServiceProvider.GetRequiredService<LineCountViewModel>(),
                App.ServiceProvider.GetRequiredService<IClipboardService>(),
                App.ServiceProvider.GetRequiredService<IAutoSaveService>(),
                App.ServiceProvider.GetRequiredService<IThemeService>())
            {
                Title = Path.GetFileName(filePath),
                CloseTabCommand = CloseTabCommand,
                IsNew = true,
                IsTemporary = true,
                IsDirty = false,
                ScrollableTextEditorViewModel = new ScrollableTextEditorViewModel(
                    App.ServiceProvider.GetRequiredService<ICursorPositionService>(),
                    App.ServiceProvider.GetRequiredService<FontPropertiesViewModel>(),
                    App.ServiceProvider.GetRequiredService<LineCountViewModel>(),
                    _textBufferFactory.Create(),
                    App.ServiceProvider.GetRequiredService<IClipboardService>(),
                    App.ServiceProvider.GetRequiredService<IThemeService>(),
                    App.ServiceProvider.GetRequiredService<TextEditorViewModel>(),
                    App.ServiceProvider.GetRequiredService<ScrollManager>()
                )
            };

            _temporaryTab.ScrollableTextEditorViewModel.TextEditorViewModel.UpdateServices(_temporaryTab
                .ScrollableTextEditorViewModel.TextEditorViewModel);
            Tabs.Add(_temporaryTab);
        }

        _temporaryTab.LoadTextAsync(filePath);
        if (_temporaryTab.ScrollableTextEditorViewModel != null)
        {
            _temporaryTab.ScrollableTextEditorViewModel.TextEditorViewModel.CursorPosition = 0;
            _temporaryTab.ScrollableTextEditorViewModel.Offset = new Vector(0, 0);
        }

        _temporaryTab.Title = Path.GetFileName(filePath);
        _temporaryTab.FilePath = Path.GetFullPath(filePath);
        SelectedTab = _temporaryTab;
    }

    public void OnFileDoubleClicked(string filePath)
    {
        var existingTab = Tabs.FirstOrDefault(tab => tab.Title == Path.GetFileName(filePath));
        if (existingTab != null)
        {
            // Convert temporary tab to permanent if double-clicked
            if (existingTab == _temporaryTab) _temporaryTab = null;
            existingTab.IsTemporary = false;
            SelectedTab = existingTab;
            return;
        }
        

        var permanentTab = new TabViewModel(
            App.ServiceProvider.GetRequiredService<ICursorPositionService>(),
            App.ServiceProvider.GetRequiredService<IUndoRedoManager<TextState>>(),
            App.ServiceProvider.GetRequiredService<IFileSystemWatcherFactory>(),
            _textBufferFactory,
            App.ServiceProvider.GetRequiredService<FontPropertiesViewModel>(),
            App.ServiceProvider.GetRequiredService<LineCountViewModel>(),
            App.ServiceProvider.GetRequiredService<IClipboardService>(),
            App.ServiceProvider.GetRequiredService<IAutoSaveService>(),
            App.ServiceProvider.GetRequiredService<IThemeService>())
        {
            Title = Path.GetFileName(filePath),
            CloseTabCommand = CloseTabCommand,
            IsNew = true,
            ScrollableTextEditorViewModel = new ScrollableTextEditorViewModel(
                App.ServiceProvider.GetRequiredService<ICursorPositionService>(),
                App.ServiceProvider.GetRequiredService<FontPropertiesViewModel>(),
                App.ServiceProvider.GetRequiredService<LineCountViewModel>(),
                _textBufferFactory.Create(),
                App.ServiceProvider.GetRequiredService<IClipboardService>(),
                App.ServiceProvider.GetRequiredService<IThemeService>(),
                App.ServiceProvider.GetRequiredService<TextEditorViewModel>(),
                App.ServiceProvider.GetRequiredService<ScrollManager>()
            )
        };

        permanentTab.ScrollableTextEditorViewModel.TextEditorViewModel.UpdateServices(permanentTab
            .ScrollableTextEditorViewModel.TextEditorViewModel);
        permanentTab.LoadTextAsync(filePath);
        permanentTab.FilePath = Path.GetFullPath(filePath);
        Tabs.Add(permanentTab);
        SelectedTab = permanentTab;
    }

    private async Task SaveFileAsync(TabViewModel tab)
    {
        try
        {
            await tab.SaveAsync();
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error saving file: {ex.Message}");
            await ShowErrorDialogAsync("Failed to save the file. Please try again.");
        }
    }

    private async Task ShowErrorDialogAsync(string message)
    {
        await _dialogService.ShowErrorDialogAsync(this, message);
    }

    public async Task RestoreSpecificBackup(string backupId)
    {
        if (SelectedTab != null)
            try
            {
                await SelectedTab.RestoreFromBackupAsync(backupId);
                // Update UI or show success message
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to restore backup: {ex.Message}");
            }
    }
    
    private void SaveCurrentFile()
    {
        if (SelectedTab != null && !string.IsNullOrEmpty(SelectedTab.FilePath))
        {
            File.WriteAllText(SelectedTab.FilePath,
                SelectedTab.ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.Text);
            SelectedTab.OriginalText = SelectedTab.ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.Text;
            SelectedTab.IsDirty = false;
        }
    }
}