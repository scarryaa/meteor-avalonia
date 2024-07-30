using System.Collections.ObjectModel;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Services;

public class TabService : ITabService
{
    private readonly ITabViewModelFactory _tabViewModelFactory;
    private readonly List<ITabViewModel> _tabHistory = [];
    private ITabViewModel _previousActiveTab;

    public TabService(ITabViewModelFactory tabViewModelFactory)
    {
        _tabViewModelFactory = tabViewModelFactory;
    }

    public ObservableCollection<ITabViewModel?> Tabs { get; } = [];
    public ITabViewModel ActiveTab { get; private set; }

    public event EventHandler<ITabViewModel?> TabAdded;
    public event EventHandler<ITabViewModel?> TabRemoved;
    public event EventHandler<ITabViewModel?> ActiveTabChanged;

    public ITabViewModel AddTab(IEditorViewModel editorViewModel, ITabViewModelConfig tabConfig, string filePath,
        string fileName,
        string initialContent = "")
    {
        var tabViewModel = CreateTabViewModel(editorViewModel, tabConfig, filePath, fileName, initialContent);
        Tabs.Add(tabViewModel);
        TabAdded?.Invoke(this, tabViewModel);
        SetActiveTab(tabViewModel);

        return tabViewModel;
    }

    private ITabViewModel CreateTabViewModel(IEditorViewModel editorViewModel, ITabViewModelConfig tabConfig,
        string filePath, string fileName, string initialContent)
    {
        var tabViewModel = _tabViewModelFactory.Create(editorViewModel, tabConfig, filePath, fileName);
        tabViewModel?.LoadContent(initialContent);
        return tabViewModel;
    }

    public ITabViewModel GetPreviousActiveTab()
    {
        return _previousActiveTab;
    }

    public void RemoveTab(ITabViewModel? tab)
    {
        if (Tabs.Remove(tab))
        {
            TabRemoved?.Invoke(this, tab);
            _tabHistory.Remove(tab);
            UpdateActiveTabAfterRemoval(tab);
        }
    }

    private void UpdateActiveTabAfterRemoval(ITabViewModel? removedTab)
    {
        if (ActiveTab == removedTab)
        {
            var newActiveTab = _tabHistory.LastOrDefault(t => Tabs.Contains(t)) ?? Tabs.LastOrDefault();
            SetActiveTab(newActiveTab);
        }

        if (Tabs.Count == 0) SetActiveTab(null);
    }

    public void SetActiveTab(ITabViewModel? tab)
    {
        if (ShouldUpdateActiveTab(tab))
            UpdateActiveTab(tab);
        else if (tab == null) ClearActiveTab();
    }

    private bool ShouldUpdateActiveTab(ITabViewModel? tab)
    {
        return tab != null && tab != ActiveTab && Tabs.Contains(tab);
    }

    private void UpdateActiveTab(ITabViewModel tab)
    {
        DeactivateCurrentTab();
        ActiveTab = tab;
        ActiveTab.IsActive = true;

        UpdateTabHistory(tab);

        ActiveTabChanged?.Invoke(this, tab);
    }

    private void DeactivateCurrentTab()
    {
        if (ActiveTab != null)
        {
            ActiveTab.IsActive = false;
            _previousActiveTab = ActiveTab;
        }
    }

    private void UpdateTabHistory(ITabViewModel tab)
    {
        _tabHistory.Remove(tab);
        _tabHistory.Add(tab);
    }

    private void ClearActiveTab()
    {
        _previousActiveTab = ActiveTab;
        ActiveTab = null;
    }
}