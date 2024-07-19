using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Factories;

namespace meteor.UI.ViewModels;

public class TabViewModel : ITabViewModel
{
    private ObservableCollection<ITabItemViewModel> _tabs;
    private ITabItemViewModel _selectedTab;

    public event PropertyChangedEventHandler PropertyChanged;

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
                    _selectedTab.IsSelected = true;

                OnPropertyChanged();
            }
        }
    }

    public ICommand AddTabCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ICommand CloseAllTabsCommand { get; }
    public ICommand CloseOtherTabsCommand { get; }

    public TabViewModel(IEditorViewModelFactory editorViewModelFactory, ICommandFactory commandFactory)
    {
        _tabs = new ObservableCollection<ITabItemViewModel>();
        AddTabCommand = commandFactory.CreateCommand(() => AddTab(editorViewModelFactory));
        CloseTabCommand = commandFactory.CreateCommand<ITabItemViewModel>(CloseTab);
        CloseAllTabsCommand = commandFactory.CreateCommand(() => CloseAllTabs());
        CloseOtherTabsCommand = commandFactory.CreateCommand(() => CloseOtherTabs());
    }

    private void AddTab(IEditorViewModelFactory editorViewModelFactory)
    {
        var newEditor = editorViewModelFactory.Create();
        var newTab = new TabItemViewModel($"Tab {Tabs.Count + 1}", newEditor);
        Tabs.Add(newTab);
        SelectedTab = newTab;
    }

    private void CloseTab(ITabItemViewModel tab)
    {
        if (Tabs.Count > 0)
        {
            var index = Tabs.IndexOf(tab);
            if (SelectedTab == tab)
                // Select the previous tab if available
                SelectedTab = Tabs.Count > 1 ? Tabs[Math.Max(0, index - 1)] : null;
            Tabs.Remove(tab);
            tab.Dispose();
        }
    }

    private void CloseAllTabs()
    {
        // Deselect the selected tab before clearing the list
        SelectedTab = null;
        Tabs.Clear();
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
            }
        }
    }

    public void Dispose()
    {
        foreach (var tab in Tabs) tab.Dispose();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}