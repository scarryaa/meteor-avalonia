using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Factories;

namespace meteor.UI.ViewModels;

public sealed class TabViewModel : ITabViewModel
{
    private readonly ITabService _tabService;
    private ObservableCollection<ITabItemViewModel> _tabs;
    private ITabItemViewModel _selectedTab;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ITabItemViewModel> Tabs
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

    public ITabItemViewModel SelectedTab
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
                    _tabService.SwitchTab(Tabs.IndexOf(_selectedTab) + 1);
                }

                OnPropertyChanged();
            }
        }
    }

    public ICommand AddTabCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ICommand CloseAllTabsCommand { get; }
    public ICommand CloseOtherTabsCommand { get; }

    public TabViewModel(IEditorViewModelFactory editorViewModelFactory, ICommandFactory commandFactory,
        ITabService tabService)
    {
        _tabService = tabService;
        _tabs = new ObservableCollection<ITabItemViewModel>();

        AddTabCommand = commandFactory.CreateCommand(() => AddTab(editorViewModelFactory));
        CloseTabCommand = commandFactory.CreateCommand<ITabItemViewModel>(CloseTab);
        CloseAllTabsCommand = commandFactory.CreateCommand(() => CloseAllTabs());
        CloseOtherTabsCommand = commandFactory.CreateCommand(() => CloseOtherTabs());
    }

    private void AddTab(IEditorViewModelFactory editorViewModelFactory)
    {
        var newEditor = editorViewModelFactory.Create();
        var tabIndex = _tabs.Count + 1;
        _tabService.RegisterTab(tabIndex, newEditor.TextBufferService);
        var newTab = new TabItemViewModel($"Tab {tabIndex}", newEditor);
        _tabs.Add(newTab);
        SelectedTab = newTab;
    }

    private void CloseTab(ITabItemViewModel tab)
    {
        if (Tabs.Count > 0)
        {
            var index = Tabs.IndexOf(tab) + 1;
            if (SelectedTab == tab)
                // Select the previous tab if available
                SelectedTab = Tabs.Count > 1 ? Tabs[Math.Max(0, index - 2)] : null;
            
            Tabs.Remove(tab);

            // Unregister the tab from the service
            _tabService.RegisterTab(index, null);
        }
    }

    private void CloseAllTabs()
    {
        // Deselect the selected tab before clearing the list
        SelectedTab = null;
        foreach (var tab in Tabs.ToList())
        {
            Tabs.Remove(tab);
            tab.Dispose();
            // Unregister the tab from the service
            var index = Tabs.Count + 1;
            _tabService.RegisterTab(index, null);
        }
    }

    private void CloseOtherTabs()
    {
        if (SelectedTab != null)
        {
            var tabsToRemove = Tabs.Where(t => t != SelectedTab).ToList();
            foreach (var tab in tabsToRemove)
            {
                Tabs.Remove(tab);
                tab.Dispose();
                // Unregister the tab from the service
                var index = Tabs.Count + 1;
                _tabService.RegisterTab(index, null);
            }
        }
    }

    public void Dispose()
    {
        foreach (var tab in Tabs.ToList())
        {
            Tabs.Remove(tab);
            tab.Dispose();
            // Unregister the tab from the service
            var index = Tabs.Count + 1;
            _tabService.RegisterTab(index, null);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}