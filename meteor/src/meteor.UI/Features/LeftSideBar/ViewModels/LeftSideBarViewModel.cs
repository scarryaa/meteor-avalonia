using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Features.LeftSideBar.ViewModels;

public class LeftSideBarViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly IThemeManager _themeManager;
    private string _currentDirectory;
    private string _currentView;
    private ObservableCollection<string> _sideBarItems;

    public LeftSideBarViewModel(IFileService fileService, IThemeManager themeManager)
    {
        _fileService = fileService;
        _themeManager = themeManager;
        _sideBarItems = new ObservableCollection<string> { "Files", "Search", "Source Control" };
        _currentView = "Files";

        InitializeCommands();
    }

    public ObservableCollection<string> SideBarItems
    {
        get => _sideBarItems;
        set => SetProperty(ref _sideBarItems, value);
    }

    public string CurrentDirectory
    {
        get => _currentDirectory;
        set => SetProperty(ref _currentDirectory, value);
    }

    public string CurrentView
    {
        get => _currentView;
        set
        {
            if (SetProperty(ref _currentView, value)) ViewChanged?.Invoke(this, value);
        }
    }

    public IRelayCommand FileSelectedCommand { get; private set; }
    public IRelayCommand DirectoryOpenedCommand { get; private set; }
    public IRelayCommand SetDirectoryCommand { get; private set; }
    public IRelayCommand<string> SwitchViewCommand { get; private set; }

    public event EventHandler<string> FileSelected;
    public event EventHandler<string> DirectoryOpened;
    public event EventHandler<string> ViewChanged;

    private void InitializeCommands()
    {
        FileSelectedCommand = new RelayCommand<string>(OnFileSelected);
        DirectoryOpenedCommand = new RelayCommand<string>(OnDirectoryOpened);
        SetDirectoryCommand = new RelayCommand<string>(SetDirectory);
        SwitchViewCommand = new RelayCommand<string>(SwitchView);
    }

    private void OnFileSelected(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath)) FileSelected?.Invoke(this, filePath);
    }

    private void OnDirectoryOpened(string directoryPath)
    {
        if (!string.IsNullOrEmpty(directoryPath)) DirectoryOpened?.Invoke(this, directoryPath);
    }

    private void SetDirectory(string path)
    {
        CurrentDirectory = path;
    }

    private void SwitchView(string view)
    {
        if (SideBarItems.Contains(view)) CurrentView = view;
    }
}