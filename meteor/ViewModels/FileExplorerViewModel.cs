using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using meteor.Interfaces;
using ReactiveUI;
using File = meteor.Models.File;

namespace meteor.ViewModels;

public class FileExplorerViewModel : ViewModelBase
{
    private File _selectedItem;
    private string _selectedPath;
    private IBrush _selectedBrush;
    private IBrush _iconBrush;
    private IBrush _outlineBrush;
    private readonly IThemeService _themeService;

    public FileExplorerViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        SelectPathCommand = ReactiveCommand.CreateFromTask(SelectPathAsync,
            this.WhenAnyValue(x => x.IsPathSelected).Select(_ => true));

        UpdateBrushes();
        _themeService.ThemeChanged += OnThemeChanged;
    }

    public event EventHandler? InvalidateRequired;

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateBrushes();
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }

    public ObservableCollection<File> Items { get; } = new();

    public File SelectedItem
    {
        get => _selectedItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedItem, value);
            SelectedPath = _selectedItem?.Path;
        }
    }

    public string SelectedPath
    {
        get => _selectedPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPath, value);
            ReloadDirectory(_selectedPath);
        }
    }

    public bool IsPathSelected => !string.IsNullOrEmpty(SelectedPath);

    public ReactiveCommand<Unit, Unit> SelectPathCommand { get; }

    public IBrush SelectedBrush
    {
        get => _selectedBrush;
        set => this.RaiseAndSetIfChanged(ref _selectedBrush, value);
    }

    public IBrush IconBrush
    {
        get => _iconBrush;
        set => this.RaiseAndSetIfChanged(ref _iconBrush, value);
    }

    public IBrush OutlineBrush
    {
        get => _outlineBrush;
        set => this.RaiseAndSetIfChanged(ref _outlineBrush, value);
    }

    private void LoadDirectory(string path)
    {
        var rootDirectory = new DirectoryInfo(path);
        var rootItem = new File
        {
            Name = rootDirectory.Name,
            Path = rootDirectory.FullName,
            IsDirectory = true
        };
        Items.Clear();
        Items.Add(rootItem);
        LoadSubItems(rootItem, rootDirectory);

        this.RaisePropertyChanged(nameof(Items));
    }

    private void UpdateBrushes()
    {
        SelectedBrush = GetResourceBrush("FileExplorerSelected");
        OutlineBrush = GetResourceBrush("FileExplorerOutline");
        IconBrush = GetResourceBrush("FileExplorerIcon");
    }

    public virtual void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }

    private IBrush GetResourceBrush(string resourceKey)
    {
        return _themeService.GetResourceBrush(resourceKey);
    }

    private void LoadSubItems(File parentItem, DirectoryInfo directoryInfo)
    {
        foreach (var directory in directoryInfo.GetDirectories())
        {
            var subItem = new File { Name = directory.Name, Path = directory.FullName, IsDirectory = true };
            parentItem.Items.Add(subItem);
            LoadSubItems(subItem, directory);
        }

        foreach (var file in directoryInfo.GetFiles())
        {
            var subItem = new File { Name = file.Name, Path = file.FullName, IsDirectory = false };
            parentItem.Items.Add(subItem);
        }
    }

    private void ReloadDirectory(string path)
    {
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            Items.Clear();
            LoadDirectory(path);
            this.RaisePropertyChanged(nameof(Items));
            this.RaisePropertyChanged(nameof(IsPathSelected));
        }
    }

    private async Task SelectPathAsync()
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
            if (result != null) SelectedPath = result;
        }
    }
}
