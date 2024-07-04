using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using meteor.Interfaces;
using meteor.Models;
using ReactiveUI;

namespace meteor.ViewModels;

public class CommandPaletteViewModel : ReactiveObject
{
    private string _searchText;
    private ObservableCollection<Command> _filteredCommands;
    private Command _selectedCommand;
    private bool _isVisible;
    private readonly MainWindowViewModel _mainViewModel;
    private int _selectedIndex;
    private readonly IThemeService _themeService;

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ICommand CloseCommand { get; }
    public ICommand SelectNextCommand { get; }
    public ICommand SelectPreviousCommand { get; }
    public ICommand ExecuteSelectedCommand { get; }
    public ICommand ExecuteCommand { get; }

    public event EventHandler? FocusRequested;
    public event EventHandler? ThemeChanged;

    public ObservableCollection<Command> Commands { get; }

    public ObservableCollection<Command> FilteredCommands
    {
        get => _filteredCommands;
        private set => this.RaiseAndSetIfChanged(ref _filteredCommands, value);
    }

    public Command SelectedCommand
    {
        get => _selectedCommand;
        set => this.RaiseAndSetIfChanged(ref _selectedCommand, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            this.RaiseAndSetIfChanged(ref _isVisible, value);
            if (value) FocusRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public CommandPaletteViewModel(MainWindowViewModel mainViewModel)
    {
        _themeService = mainViewModel.ThemeService;
        _isVisible = false;
        CloseCommand = ReactiveCommand.Create(Close);
        SelectNextCommand = ReactiveCommand.Create(SelectNext);
        SelectPreviousCommand = ReactiveCommand.Create(SelectPrevious);
        ExecuteSelectedCommand = ReactiveCommand.Create(ExecuteSelected);
        ExecuteCommand = ReactiveCommand.Create<Command>(Execute);
        _mainViewModel = mainViewModel;
        Commands = new ObservableCollection<Command>
        {
            new()
            {
                Name = "New File",
                Action = () =>
                {
                    _mainViewModel.NewTabCommand.Execute(null);
                    Close();
                }
            },
            new()
            {
                Name = "Open Folder",
                Action = () =>
                {
                    _mainViewModel.OpenFolderCommand.Execute();
                    Close();
                }
            },
            new()
            {
                Name = "Save File",
                Action = () =>
                {
                    _mainViewModel.SaveCommand.Execute().Subscribe();
                    Close();
                }
            },
            new()
            {
                Name = "Set Light Theme",
                Action = () =>
                {
                    mainViewModel.SetLightThemeCommand.Execute().Subscribe();
                    Close();
                }
            },
            new()
            {
                Name = "Set Dark Theme",
                Action = () =>
                {
                    mainViewModel.SetDarkThemeCommand.Execute().Subscribe();
                    Close();
                }
            }
        };

        FilteredCommands = new ObservableCollection<Command>(Commands);

        _themeService.ThemeChanged += OnThemeChanged;

        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Select(term => term?.Trim())
            .DistinctUntilChanged()
            .Subscribe(FilterCommands);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void FilterCommands(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            FilteredCommands = new ObservableCollection<Command>(Commands);
        else
            FilteredCommands = new ObservableCollection<Command>(
                Commands.Where(c => c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)));

        SelectedIndex = FilteredCommands.Count > 0 ? 0 : -1;
        SelectedCommand = SelectedIndex >= 0 ? FilteredCommands[SelectedIndex] : null;
    }

    private void ExecuteSelected()
    {
        if (SelectedCommand != null)
        {
            SelectedCommand.Action();
            Close();
        }
    }

    private void SelectNext()
    {
        if (FilteredCommands.Count > 0)
        {
            SelectedIndex = (SelectedIndex + 1) % FilteredCommands.Count;
            SelectedCommand = FilteredCommands[SelectedIndex];
        }
    }

    private void SelectPrevious()
    {
        if (FilteredCommands.Count > 0)
        {
            SelectedIndex = (SelectedIndex - 1 + FilteredCommands.Count) % FilteredCommands.Count;
            SelectedCommand = FilteredCommands[SelectedIndex];
        }
    }

    private void Execute(Command command)
    {
        if (command != null)
            command.Action();
        else
            Console.WriteLine("Execute called with null command");

        Close();
    }

    public void Close()
    {
        _mainViewModel.IsCommandPaletteVisible = false;
        SearchText = string.Empty;
        SelectedCommand = null;
        SelectedIndex = -1;
    }
}