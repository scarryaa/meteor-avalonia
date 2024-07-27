using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using meteor.Core.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.UI.ViewModels;

public class MainWindowViewModel : ObservableObject
{
    private readonly ITabService _tabService;
    private readonly IEditorInstanceFactory _editorInstanceFactory;
    private ITabViewModel _activeTab;

    public ICommand OpenNewTabCommand { get; }
    public ICommand CloseTabCommand { get; }

    public MainWindowViewModel(ITabService tabService, IEditorInstanceFactory editorInstanceFactory)
    {
        _tabService = tabService;
        _editorInstanceFactory = editorInstanceFactory;

        OpenNewTabCommand = new RelayCommand(OpenNewTab);
        CloseTabCommand = new RelayCommand<ITabViewModel>(CloseTab);

        // Subscribe to TabService events
        _tabService.TabAdded += (sender, tab) =>
        {
            OnPropertyChanged(nameof(Tabs));
            Debug.WriteLine($"Tab added: {tab.FileName}");
            LogTabState();
        };
        _tabService.TabRemoved += (sender, tab) =>
        {
            OnPropertyChanged(nameof(Tabs));
            Debug.WriteLine($"Tab removed: {tab.FileName}");
            LogTabState();
        };
        _tabService.ActiveTabChanged += (sender, tab) =>
        {
            ActiveTab = tab;
            Debug.WriteLine($"Active tab changed to: {tab?.FileName ?? "null"}");
            LogTabState();
        };

        ((INotifyCollectionChanged)_tabService.Tabs).CollectionChanged +=
            (sender, args) => OnPropertyChanged(nameof(Tabs));
    }

    public ObservableCollection<ITabViewModel> Tabs => _tabService.Tabs;

    public ITabViewModel ActiveTab
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

    public void OpenNewTab()
    {
        var newEditorInstance = _editorInstanceFactory.Create();
        _tabService.AddTab(newEditorInstance.EditorViewModel, new TabConfig(_tabService),
            $"Untitled {_tabService.Tabs.Count + 1}");
        Debug.WriteLine($"New tab opened: Untitled {_tabService.Tabs.Count}");
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

    private void LogTabState()
    {
        Debug.WriteLine("Current tab state:");
        foreach (var tab in Tabs) Debug.WriteLine($"- {tab.FileName} {(tab == ActiveTab ? "(Active)" : "")}");
    }
}