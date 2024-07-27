using System.Collections.ObjectModel;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Services;

public class TabService : ITabService
{
    private readonly ITabViewModelFactory _tabViewModelFactory;

    public TabService(ITabViewModelFactory tabViewModelFactory)
    {
        _tabViewModelFactory = tabViewModelFactory;
    }

    public ObservableCollection<ITabViewModel> Tabs { get; } = new();
    public ITabViewModel ActiveTab { get; private set; }

    public event EventHandler<ITabViewModel> TabAdded;
    public event EventHandler<ITabViewModel> TabRemoved;
    public event EventHandler<ITabViewModel> ActiveTabChanged;

    public void AddTab(IEditorViewModel editorViewModel, ITabViewModelConfig tabConfig, string fileName)
    {
        var tabViewModel = _tabViewModelFactory.Create(editorViewModel, tabConfig, fileName);
        Tabs.Add(tabViewModel);
        TabAdded?.Invoke(this, tabViewModel);
        if (Tabs.Count == 1) SetActiveTab(tabViewModel);
    }

    public void RemoveTab(ITabViewModel tab)
    {
        if (Tabs.Remove(tab))
        {
            TabRemoved?.Invoke(this, tab);
            if (ActiveTab == tab) SetActiveTab(Tabs.Count > 0 ? Tabs[0] : null);
        }
    }

    public void SetActiveTab(ITabViewModel tab)
    {
        if (tab != ActiveTab && Tabs.Contains(tab))
        {
            if (ActiveTab != null) ActiveTab.IsActive = false;
            ActiveTab = tab;
            ActiveTab.IsActive = true;
            ActiveTabChanged?.Invoke(this, tab);
        }
    }
}