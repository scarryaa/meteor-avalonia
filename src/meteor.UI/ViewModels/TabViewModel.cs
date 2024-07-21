using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Events;
using meteor.UI.Factories;

namespace meteor.UI.ViewModels;

public sealed class TabViewModel : ITabViewModel
{
    private readonly ITabService _tabService;
    private readonly IEditorViewModelFactory _editorViewModelFactory;
    private ITabItemViewModel? _selectedTab;
    private ObservableCollection<ITabItemViewModel?> _tabs;

    public TabViewModel(IEditorViewModelFactory editorViewModelFactory, ICommandFactory commandFactory,
        ITabService tabService)
    {
        _tabService = tabService;
        _editorViewModelFactory = editorViewModelFactory;
        _tabs = new ObservableCollection<ITabItemViewModel?>();

        AddTabCommand = commandFactory.CreateCommand(AddTab);
        CloseTabCommand = commandFactory.CreateCommand<ITabItemViewModel>(CloseTab);
        CloseAllTabsCommand = commandFactory.CreateCommand(CloseAllTabs);
        CloseOtherTabsCommand = commandFactory.CreateCommand<ITabItemViewModel>(CloseOtherTabs);

        _tabService.TabChanged += OnTabChanged;

        RefreshTabs();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ITabItemViewModel?> Tabs
    {
        get => _tabs;
        set
        {
            if (_tabs != value)
            {
                _tabs = value;
                OnPropertyChanged();
            }
        }
    }

    public ITabItemViewModel? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab != value)
            {
                if (_selectedTab != null)
                    _selectedTab.IsSelected = false;

                _selectedTab = value;

                if (_selectedTab != null)
                {
                    _selectedTab.IsSelected = true;
                    _tabService.SwitchTab(_selectedTab.Index);
                }

                OnPropertyChanged();
            }
        }
    }

    public ICommand AddTabCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ICommand CloseAllTabsCommand { get; }
    public ICommand CloseOtherTabsCommand { get; }

    public void Dispose()
    {
        CloseAllTabs();
        _tabService.TabChanged -= OnTabChanged;
    }

    private void AddTab()
    {
        var newEditor = _editorViewModelFactory.Create();
        var newTabInfo = _tabService.AddTab(newEditor.TextBufferService, newEditor);
        var newTabViewModel = new TabItemViewModel(newTabInfo.Index, newTabInfo.Title, newEditor);
        _tabs.Add(newTabViewModel);
        SelectedTab = newTabViewModel;
    }

    private void CloseTab(ITabItemViewModel tab)
    {
        _tabService.CloseTab(tab.Index);
        _tabs.Remove(tab);
        tab.Dispose();
        SelectedTab = _tabs.FirstOrDefault(t => t.Index == _tabService.GetActiveTab()?.Index);
    }

    private void CloseAllTabs()
    {
        _tabService.CloseAllTabs();
        foreach (var tab in _tabs) tab.Dispose();
        _tabs.Clear();
        SelectedTab = null;
    }

    private void CloseOtherTabs(ITabItemViewModel? tabToKeep)
    {
        if (tabToKeep != null)
        {
            _tabService.CloseOtherTabs(tabToKeep.Index);
            var tabsToRemove = _tabs.Where(t => t != tabToKeep).ToList();
            foreach (var tab in tabsToRemove)
            {
                _tabs.Remove(tab);
                tab?.Dispose();
            }

            SelectedTab = tabToKeep;
        }
    }

    private void RefreshTabs()
    {
        foreach (var tab in _tabs) tab.Dispose();
        _tabs.Clear();
        foreach (var tabInfo in _tabService.GetAllTabs())
        {
            var editorViewModel = _editorViewModelFactory.Create();
            var tabViewModel = new TabItemViewModel(tabInfo.Index, tabInfo.Title, editorViewModel);
            _tabs.Add(tabViewModel);
        }

        var activeTabInfo = _tabService.GetActiveTab();
        SelectedTab = activeTabInfo != null ? _tabs.FirstOrDefault(t => t.Index == activeTabInfo.Index) : null;
    }

    private void OnTabChanged(object sender, TabChangedEventArgs e)
    {
        var activeTabInfo = e.NewTab;
        if (activeTabInfo != null)
        {
            var activeTab = _tabs.FirstOrDefault(t => t.Index == activeTabInfo.Index);
            if (activeTab != null)
            {
                _selectedTab = activeTab;
                _selectedTab.IsSelected = true;

                OnPropertyChanged(nameof(SelectedTab));
            }
        }
        else
        {
            _selectedTab = null;
            OnPropertyChanged(nameof(SelectedTab));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
