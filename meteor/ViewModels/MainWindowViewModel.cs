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
using meteor.Interfaces;
using meteor.Models;
using meteor.Views;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using File = System.IO.File;

namespace meteor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly Stack<TabViewModel> _tabSelectionHistory;
    private TabViewModel? _selectedTab;
    private double _windowWidth;
    private double _windowHeight;
    private bool _isDialogOpen;
    private string _selectedPath;
    private bool _suppressHistoryTracking;
    private TabViewModel _temporaryTab;
    private readonly ITextBufferFactory _textBufferFactory;

    public MainWindowViewModel(
        TitleBarViewModel titleBarViewModel,
        StatusPaneViewModel statusPaneViewModel,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel,
        ICursorPositionService cursorPositionService,
        FileExplorerViewModel fileExplorerViewModel,
        ITextBufferFactory textBufferFactory)
    {
        _textBufferFactory = textBufferFactory;
        StatusPaneViewModel = statusPaneViewModel;
        FileExplorerViewModel = fileExplorerViewModel;
        TitleBarViewModel = titleBarViewModel;

        Tabs = new ObservableCollection<TabViewModel>();

        NewTabCommand = ReactiveCommand.Create(NewTab);
        CloseTabCommand = ReactiveCommand.Create<TabViewModel>(async tab => await CloseTabAsync(tab));
        SaveCommand = ReactiveCommand.Create(SaveCurrentFile);
        OpenFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
        ShowSaveDialogCommand = ReactiveCommand.Create(() => IsDialogOpen = true);
        HideSaveDialogCommand = ReactiveCommand.Create(() => IsDialogOpen = false);

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

    public TabViewModel SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab != null)
            {
                _selectedTab.SavedVerticalOffset = _selectedTab.ScrollableTextEditorViewModel.VerticalOffset;
                _selectedTab.SavedHorizontalOffset = _selectedTab.ScrollableTextEditorViewModel.HorizontalOffset;
            }

            this.RaiseAndSetIfChanged(ref _selectedTab, value);

            if (_selectedTab != null)
                Dispatcher.UIThread.Post(() =>
                {
                    _selectedTab.ScrollableTextEditorViewModel.TextEditorViewModel.Focus();
                    _selectedTab.ScrollableTextEditorViewModel.VerticalOffset = _selectedTab.SavedVerticalOffset;
                    _selectedTab.ScrollableTextEditorViewModel.HorizontalOffset = _selectedTab.SavedHorizontalOffset;

                    _selectedTab.ScrollableTextEditorViewModel.UpdateLongestLineWidth();
                }, DispatcherPriority.Render);
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

    public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, bool> ShowSaveDialogCommand { get; }
    public ReactiveCommand<Unit, bool> HideSaveDialogCommand { get; }

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

    private async Task<bool?> ShowSaveConfirmationDialogAsync()
    {
        var dialog = new SaveConfirmationDialog(this);
        var result = await dialog.ShowDialog<bool?>(GetMainWindow());
        return result;
    }

    private Window GetMainWindow()
    {
        return Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
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

        var newTab = new TabViewModel(
            cursorPositionService,
            undoRedoManager,
            fileSystemWatcherFactory,
            _textBufferFactory,
            fontPropertiesViewModel,
            lineCountViewModel,
            clipboardService)
        {
            Title = $"Untitled {Tabs.Count + 1}",
            ScrollableTextEditorViewModel = new ScrollableTextEditorViewModel(
                cursorPositionService,
                fontPropertiesViewModel,
                lineCountViewModel,
                textBuffer,
                clipboardService),
            CloseTabCommand = CloseTabCommand
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

        if (tab.IsDirty)
        {
            ShowSaveDialogCommand.Execute().Subscribe();

            var result = await ShowSaveConfirmationDialogAsync();
            HideSaveDialogCommand.Execute().Subscribe();

            if (result == true)
                SaveFile(tab);
            else if (result == null) return;
        }

        Tabs.Remove(tab);

        if (tab == _temporaryTab)
            _temporaryTab = null;

        if (tab == SelectedTab)
        {
            // Remove the closed tab from the selection history
            var tempStack = new Stack<TabViewModel>();
            while (_tabSelectionHistory.Count > 0)
            {
                var topTab = _tabSelectionHistory.Pop();
                if (topTab != tab)
                    tempStack.Push(topTab);
            }

            while (tempStack.Count > 0)
                _tabSelectionHistory.Push(tempStack.Pop());

            // Temporarily suppress history tracking for auto-selection
            _suppressHistoryTracking = true;

            // Set the last selected tab if available
            if (_tabSelectionHistory.Count > 0)
                SelectedTab = _tabSelectionHistory.Pop();
            else if (Tabs.Count > 0)
                SelectedTab = Tabs[0];

            _suppressHistoryTracking = false;
        }
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
                App.ServiceProvider.GetRequiredService<IClipboardService>())
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
                    App.ServiceProvider.GetRequiredService<IClipboardService>()
                )
            };

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
            App.ServiceProvider.GetRequiredService<IClipboardService>())
        {
            Title = Path.GetFileName(filePath),
            CloseTabCommand = CloseTabCommand,
            IsNew = true,
            ScrollableTextEditorViewModel = new ScrollableTextEditorViewModel(
                App.ServiceProvider.GetRequiredService<ICursorPositionService>(),
                App.ServiceProvider.GetRequiredService<FontPropertiesViewModel>(),
                App.ServiceProvider.GetRequiredService<LineCountViewModel>(),
                _textBufferFactory.Create(),
                App.ServiceProvider.GetRequiredService<IClipboardService>()
            )
        };

        permanentTab.LoadTextAsync(filePath);
        permanentTab.FilePath = Path.GetFullPath(filePath);
        Tabs.Add(permanentTab);
        SelectedTab = permanentTab;
    }

    private void SaveFile(TabViewModel tab)
    {
        if (tab != null && !string.IsNullOrEmpty(tab.FilePath))
        {
            File.WriteAllText(tab.FilePath, tab.ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.Text);
            tab.IsDirty = false;
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