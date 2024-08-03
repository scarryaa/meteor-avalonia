using System.Collections.ObjectModel;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Services;

public class TabService : ITabService
{
    private readonly List<ITabViewModel> _tabHistory = [];
    private readonly ITabViewModelFactory _tabViewModelFactory;
    private readonly IThemeManager _themeManager;
    private ITabViewModel _previousActiveTab;

    public TabService(ITabViewModelFactory tabViewModelFactory, IThemeManager themeManager)
    {
        _tabViewModelFactory = tabViewModelFactory;
        _themeManager = themeManager;
    }

    public ObservableCollection<ITabViewModel?> Tabs { get; } = [];
    public ITabViewModel ActiveTab { get; private set; }

    public event EventHandler<ITabViewModel?> TabAdded;
    public event EventHandler<ITabViewModel?> TabRemoved;
    public event EventHandler<ITabViewModel?> ActiveTabChanged;

    public ITabViewModel AddTab(IEditorViewModel editorViewModel, ITabViewModelConfig tabConfig, string filePath,
        string fileName, string initialContent = "")
    {
        var tabViewModel = _tabViewModelFactory.Create(editorViewModel, tabConfig, filePath, fileName, _themeManager);
        tabViewModel?.LoadContent(initialContent);
        Tabs.Add(tabViewModel);
        TabAdded?.Invoke(this, tabViewModel);
        SetActiveTab(tabViewModel);
        return tabViewModel;
    }

    public ITabViewModel GetPreviousActiveTab() => _previousActiveTab;

    public void RemoveTab(ITabViewModel? tab)
    {
        if (!Tabs.Remove(tab)) return;
        TabRemoved?.Invoke(this, tab);
        _tabHistory.Remove(tab);
        UpdateActiveTabAfterRemoval(tab);
    }

    public void SetActiveTab(ITabViewModel? tab)
    {
        if (tab != null && tab != ActiveTab && Tabs.Contains(tab))
            UpdateActiveTab(tab);
        else if (tab == null)
            ClearActiveTab();
    }

    private void UpdateActiveTabAfterRemoval(ITabViewModel? removedTab)
    {
        if (ActiveTab == removedTab)
        {
            var newActiveTab = _tabHistory.LastOrDefault(t => Tabs.Contains(t)) ?? Tabs.LastOrDefault();
            SetActiveTab(newActiveTab);
        }
        if (Tabs.Count == 0)
        {
            ClearActiveTab();
            ActiveTabChanged?.Invoke(this, null);
        }
    }

    private void UpdateActiveTab(ITabViewModel tab)
    {
        if (ActiveTab != null)
        {
            ActiveTab.IsActive = false;
            _previousActiveTab = ActiveTab;
        }
        ActiveTab = tab;
        ActiveTab.IsActive = true;
        _tabHistory.Remove(tab);
        _tabHistory.Add(tab);
        ActiveTabChanged?.Invoke(this, tab);
    }

    private void ClearActiveTab()
    {
        if (ActiveTab != null)
        {
            ActiveTab.IsActive = false;
            _previousActiveTab = ActiveTab;
        }
        ActiveTab = null;
    }
}