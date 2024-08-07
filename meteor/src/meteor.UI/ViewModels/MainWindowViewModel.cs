﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using meteor.Core.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;

namespace meteor.UI.ViewModels;

public class MainWindowViewModel : ObservableObject
{
    private readonly IEditorInstanceFactory _editorInstanceFactory;
    private readonly IFileDialogService _fileDialogService;
    private readonly IFileService _fileService;
    private readonly ITabService _tabService;
    private readonly IThemeManager _themeManager;
    private readonly ISettingsService _settingsService;
    private ITabViewModel? _activeTab;
    private bool _isLeftSidebarVisible;
    private bool _isRightSidebarVisible;
    private string _activeLeftSidebarView;

    public MainWindowViewModel(ITabService tabService, IEditorInstanceFactory editorInstanceFactory,
        IFileService fileService, IFileDialogService fileDialogService, IThemeManager themeManager,
        ISettingsService settingsService)
    {
        _tabService = tabService;
        _editorInstanceFactory = editorInstanceFactory;
        _fileService = fileService;
        _fileDialogService = fileDialogService;
        _themeManager = themeManager;
        _settingsService = settingsService;
        _themeManager.ThemeChanged += (sender, theme) => { OnPropertyChanged(nameof(CurrentTheme)); };

        // Load saved settings
        _isLeftSidebarVisible = _settingsService.GetSetting("IsLeftSidebarVisible", false);
        _isRightSidebarVisible = _settingsService.GetSetting("IsRightSidebarVisible", false);

        OpenNewTabCommand = new RelayCommand(OpenNewTab);
        CloseTabCommand = new RelayCommand<ITabViewModel>(CloseTab);
        SaveFileCommand = new RelayCommand(SaveFile, CanSaveFile);
        OpenFileCommand = new RelayCommand(OpenFile);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        ToggleCommandPaletteCommand = new RelayCommand(ToggleCommandPalette);
        ToggleLeftSidebarCommand = new RelayCommand(ToggleLeftSidebar);
        ToggleRightSidebarCommand = new RelayCommand(ToggleRightSidebar);
        FindInFilesCommand = new RelayCommand(FindInFiles);

        _tabService.TabAdded += (sender, tab) => { OnPropertyChanged(nameof(Tabs)); };
        _tabService.TabRemoved += (sender, tab) => { OnPropertyChanged(nameof(Tabs)); };
        _tabService.ActiveTabChanged += (sender, tab) => { ActiveTab = tab; };

        ((INotifyCollectionChanged)_tabService.Tabs).CollectionChanged +=
            (sender, args) => OnPropertyChanged(nameof(Tabs));
    }

    public ICommand OpenNewTabCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ICommand SaveFileCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand ToggleCommandPaletteCommand { get; }
    public ICommand ToggleLeftSidebarCommand { get; }
    public ICommand ToggleRightSidebarCommand { get; }
    public ICommand FindInFilesCommand { get; }

    public ObservableCollection<ITabViewModel> Tabs => _tabService.Tabs;
    public bool IsCommandPaletteVisible { get; private set; }

    public bool IsLeftSidebarVisible
    {
        get => _isLeftSidebarVisible;
        set
        {
            if (SetProperty(ref _isLeftSidebarVisible, value))
            {
                _settingsService.SetSetting("IsLeftSidebarVisible", value);
                _settingsService.SaveSettings();
            }
        }
    }

    public bool IsRightSidebarVisible
    {
        get => _isRightSidebarVisible;
        set
        {
            if (SetProperty(ref _isRightSidebarVisible, value))
            {
                _settingsService.SetSetting("IsRightSidebarVisible", value);
                _settingsService.SaveSettings();
            }
        }
    }

    public string ActiveLeftSidebarView
    {
        get => _activeLeftSidebarView;
        set
        {
            if (SetProperty(ref _activeLeftSidebarView, value))
            {
                _settingsService.SetSetting("ActiveLeftSidebarView", value);
                _settingsService.SaveSettings();
            }
        }
    }

    public ITabViewModel? ActiveTab
    {
        get => _activeTab;
        set
        {
            if (SetProperty(ref _activeTab, value))
            {
                Debug.WriteLine($"ActiveTab changed to: {value?.FileName ?? "null"}");
                _tabService.SetActiveTab(value);
            }
        }
    }

    public Theme CurrentTheme => _themeManager.CurrentTheme;

    private void ToggleCommandPalette()
    {
        IsCommandPaletteVisible = !IsCommandPaletteVisible;
        OnPropertyChanged(nameof(IsCommandPaletteVisible));
    }

    private void ToggleLeftSidebar()
    {
        IsLeftSidebarVisible = !IsLeftSidebarVisible;
        _settingsService.SetSetting("IsLeftSidebarVisible", IsLeftSidebarVisible);
        _settingsService.SaveSettings();
    }

    private void ToggleRightSidebar()
    {
        IsRightSidebarVisible = !IsRightSidebarVisible;
        _settingsService.SetSetting("IsRightSidebarVisible", IsRightSidebarVisible);
        _settingsService.SaveSettings();
    }

    private async void SaveFile()
    {
        if (ActiveTab?.EditorViewModel != null)
        {
            var filePath = ActiveTab.FilePath;

            // Check if it's a new tab or the file path is empty
            if (string.IsNullOrEmpty(filePath))
            {
                // Show save dialog
                filePath = await _fileDialogService.ShowSaveFileDialogAsync();
                if (string.IsNullOrEmpty(filePath))
                    // User cancelled the save dialog
                    return;
            }

            // Save the file
            await _fileService.SaveFileAsync(filePath, ActiveTab.EditorViewModel.Content);

            ActiveTab.SetFilePath(filePath);
            ActiveTab.SetOriginalContent(ActiveTab.EditorViewModel.Content);
            ActiveTab.IsModified = false;

            // Update last opened directory
            _settingsService.SetSetting("LastOpenedDirectory", Path.GetDirectoryName(filePath));
            _settingsService.SaveSettings();
        }
    }

    private bool CanSaveFile()
    {
        return ActiveTab is { EditorViewModel: not null, IsModified: true };
    }

    private async void OpenSettings()
    {
        var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "meteor", "settings.json");

        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath));

        // Check if settings file is already open
        var existingSettingsTab = _tabService.Tabs.FirstOrDefault(tab => tab.FilePath == settingsFilePath);
        if (existingSettingsTab != null)
        {
            // If settings file is already open, switch to that tab
            ActiveTab = existingSettingsTab;
            return;
        }

        // If settings file doesn't exist, create it with default content
        if (!File.Exists(settingsFilePath))
        {
            await File.WriteAllTextAsync(settingsFilePath, "{}");
        }

        // If settings file is not open, open it in a new tab
        var content = await _fileService.OpenFileAsync(settingsFilePath);
        var newEditorInstance = _editorInstanceFactory.Create();
        newEditorInstance.EditorViewModel.Content = content;
        _tabService.AddTab(newEditorInstance.EditorViewModel, new TabConfig(_tabService, _themeManager),
            Path.GetFullPath(settingsFilePath),
            Path.GetFileName(settingsFilePath),
            await File.ReadAllTextAsync(settingsFilePath)
        );
    }

    public void OpenNewTab()
    {
        var newEditorInstance = _editorInstanceFactory.Create();
        _tabService.AddTab(newEditorInstance.EditorViewModel, new TabConfig(_tabService, _themeManager),
            $"Untitled {_tabService.Tabs.Count + 1}",
            string.Empty,
            string.Empty
        );
        Debug.WriteLine($"New tab opened: Untitled {_tabService.Tabs.Count}");
    }

    private async void OpenFile()
    {
        var lastOpenedDirectory = _settingsService.GetSetting<string>("LastOpenedDirectory", string.Empty);
        var filePath = await _fileDialogService.ShowOpenFileDialogAsync();
        if (!string.IsNullOrEmpty(filePath))
        {
            var content = await _fileService.OpenFileAsync(filePath);
            var newEditorInstance = _editorInstanceFactory.Create();
            newEditorInstance.EditorViewModel.Content = content;
            _tabService.AddTab(newEditorInstance.EditorViewModel, new TabConfig(_tabService, _themeManager),
                Path.GetFullPath(filePath),
                Path.GetFileName(filePath),
                await File.ReadAllTextAsync(filePath)
            );

            // Update last opened directory
            _settingsService.SetSetting("LastOpenedDirectory", Path.GetDirectoryName(filePath));
            _settingsService.SaveSettings();
        }
    }

    public void CloseTab(ITabViewModel? tab = null)
    {
        if (tab == null) tab = ActiveTab;
        if (tab != null)
        {
            Debug.WriteLine($"Closing tab: {tab.FileName}");
            _tabService.RemoveTab(tab);
        }
    }

    private void FindInFiles()
    {
        if (IsLeftSidebarVisible && ActiveLeftSidebarView != "Search")
        {
            ActiveLeftSidebarView = "Search";
        }
        else if (!IsLeftSidebarVisible)
        {
            IsLeftSidebarVisible = true;
            ActiveLeftSidebarView = "Search";
        }

        // Focus on the search box in the left sidebar
        OnSearchFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler OnSearchFocusRequested;
}