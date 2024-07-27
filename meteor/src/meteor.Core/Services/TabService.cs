using System.Collections.ObjectModel;
using System.Diagnostics;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Services;

public class TabService : ITabService
{
    private readonly ITabViewModelFactory _tabViewModelFactory;
    private readonly List<ITabViewModel> _tabHistory = new();

    public TabService(ITabViewModelFactory tabViewModelFactory)
    {
        _tabViewModelFactory = tabViewModelFactory;
    }

    public ObservableCollection<ITabViewModel> Tabs { get; } = new();
    public ITabViewModel ActiveTab { get; private set; }

    public event EventHandler<ITabViewModel?> TabAdded;
    public event EventHandler<ITabViewModel?> TabRemoved;
    public event EventHandler<ITabViewModel?> ActiveTabChanged;

    public void AddTab(IEditorViewModel editorViewModel, ITabViewModelConfig tabConfig, string fileName)
    {
        var tabViewModel = _tabViewModelFactory.Create(editorViewModel, tabConfig, fileName);
        Tabs.Add(tabViewModel);
        TabAdded?.Invoke(this, tabViewModel);
        SetActiveTab(tabViewModel);
        Debug.WriteLine($"Tab added: {fileName}");
    }

    public void RemoveTab(ITabViewModel? tab)
    {
        if (Tabs.Remove(tab))
        {
            TabRemoved?.Invoke(this, tab);
            _tabHistory.Remove(tab);
            if (ActiveTab == tab)
            {
                var newActiveTab = _tabHistory.LastOrDefault(t => Tabs.Contains(t)) ??
                                   Tabs.LastOrDefault() ??
                                   Tabs.FirstOrDefault();
                SetActiveTab(newActiveTab);
            }

            if (Tabs.Count == 0) SetActiveTab(null);
        }
    }
    
    public void SetActiveTab(ITabViewModel tab)
    {
        if (tab != null && tab != ActiveTab && Tabs.Contains(tab))
        {
            if (ActiveTab != null) ActiveTab.IsActive = false;
            ActiveTab = tab;
            ActiveTab.IsActive = true;

            _tabHistory.Remove(tab);
            _tabHistory.Add(tab);

            ActiveTabChanged?.Invoke(this, tab);
        }
        else if (tab == null)
        {
            ActiveTab = null;
        }
    }
}