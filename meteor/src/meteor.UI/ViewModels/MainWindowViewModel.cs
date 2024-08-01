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
    private ITabViewModel? _activeTab;
    private bool _isLeftSidebarVisible = true;

    public MainWindowViewModel(ITabService tabService, IEditorInstanceFactory editorInstanceFactory,
        IFileService fileService, IFileDialogService fileDialogService, IThemeManager themeManager)
    {
        _tabService = tabService;
        _editorInstanceFactory = editorInstanceFactory;
        _fileService = fileService;
        _fileDialogService = fileDialogService;
        _themeManager = themeManager;
        _themeManager.ThemeChanged += (sender, theme) => { OnPropertyChanged(nameof(CurrentTheme)); };

        OpenNewTabCommand = new RelayCommand(OpenNewTab);
        CloseTabCommand = new RelayCommand<ITabViewModel>(CloseTab);
        SaveFileCommand = new RelayCommand(SaveFile, CanSaveFile);
        OpenFileCommand = new RelayCommand(OpenFile);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        ToggleCommandPaletteCommand = new RelayCommand(ToggleCommandPalette);
        ToggleLeftSidebarCommand = new RelayCommand(ToggleLeftSidebar);
        ToggleRightSidebarCommand = new RelayCommand(ToggleRightSidebar);

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

    public ObservableCollection<ITabViewModel> Tabs => _tabService.Tabs;
    public bool IsCommandPaletteVisible { get; private set; }

    public bool IsLeftSidebarVisible
    {
        get => _isLeftSidebarVisible;
        set => SetProperty(ref _isLeftSidebarVisible, value);
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

    public bool IsRightSidebarVisible { get; internal set; }
    public ICommand ToggleRightSidebarCommand { get; internal set; }

    private void ToggleCommandPalette()
    {
        IsCommandPaletteVisible = !IsCommandPaletteVisible;
        OnPropertyChanged(nameof(IsCommandPaletteVisible));
    }

    private void ToggleLeftSidebar()
    {
        IsLeftSidebarVisible = !IsLeftSidebarVisible;
        OnPropertyChanged(nameof(IsLeftSidebarVisible));
    }

    private void ToggleRightSidebar()
    {
        IsRightSidebarVisible = !IsRightSidebarVisible;
        OnPropertyChanged(nameof(IsRightSidebarVisible));
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
        }
    }

    private bool CanSaveFile()
    {
        return ActiveTab is { EditorViewModel: not null, IsModified: true };
    }

    private async void OpenSettings()
    {
        var settingsFilePath = Path.Combine(AppContext.BaseDirectory, "settings.json");

        // Check if settings file is already open
        var existingSettingsTab = _tabService.Tabs.FirstOrDefault(tab => tab.FilePath == settingsFilePath);
        if (existingSettingsTab != null)
        {
            // If settings file is already open, switch to that tab
            ActiveTab = existingSettingsTab;
            return;
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
            string.Empty,
            $"Untitled {_tabService.Tabs.Count + 1}");
        Debug.WriteLine($"New tab opened: Untitled {_tabService.Tabs.Count}");
    }

    private async void OpenFile()
    {
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
}