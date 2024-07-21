using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using meteor.Core.Models.Tabs;

namespace meteor.Core.Services;

public class TabService : ITabService
{
    private readonly Dictionary<int, TabInfo?> _tabs = new();
    private int _activeTabIndex = -1;
    private int _nextTabIndex = 1;

    public event EventHandler<TabChangedEventArgs> TabChanged;

    public ITextBufferService GetActiveTextBufferService()
    {
        if (_activeTabIndex == -1 || !_tabs.TryGetValue(_activeTabIndex, out var tab))
            return new TextBufferService();
        return tab.TextBufferService;
    }

    public void SwitchTab(int tabIndex)
    {
        if (_tabs.ContainsKey(tabIndex))
        {
            // Save current tab state
            if (_activeTabIndex != -1 && _tabs.TryGetValue(_activeTabIndex, out var currentTab))
            {
                // Nothing to do here
            }

            _activeTabIndex = tabIndex;
            OnTabChanged(new TabChangedEventArgs(GetActiveTab()));
        }
        else
        {
            throw new ArgumentException("Tab does not exist.");
        }
    }

    public TabInfo AddTab(ITextBufferService textBufferService, IEditorViewModel editorViewModel)
    {
        var tabIndex = _nextTabIndex++;
        var newTab = new TabInfo(tabIndex, $"Tab {tabIndex}", textBufferService, editorViewModel);
        _tabs[tabIndex] = newTab;

        if (_activeTabIndex == -1)
            _activeTabIndex = tabIndex;

        OnTabChanged(new TabChangedEventArgs(newTab));

        return newTab;
    }

    public void CloseTab(int tabIndex)
    {
        if (_tabs.ContainsKey(tabIndex))
        {
            _tabs.Remove(tabIndex);

            if (_activeTabIndex == tabIndex)
            {
                _activeTabIndex = _tabs.Keys.LastOrDefault(-1);
                OnTabChanged(new TabChangedEventArgs(GetActiveTab()));
            }
        }
    }

    public void CloseAllTabs()
    {
        _tabs.Clear();
        _activeTabIndex = -1;
        _nextTabIndex = 1;
        OnTabChanged(new TabChangedEventArgs(null));
    }

    public void CloseOtherTabs(int keepTabIndex)
    {
        if (_tabs.ContainsKey(keepTabIndex))
        {
            var tabToKeep = _tabs[keepTabIndex];
            _tabs.Clear();
            _tabs[keepTabIndex] = tabToKeep;
            _activeTabIndex = keepTabIndex;
            OnTabChanged(new TabChangedEventArgs(tabToKeep));
        }
    }

    public void SelectTab(int tabIndex)
    {
        if (_tabs.ContainsKey(tabIndex) && _activeTabIndex != tabIndex)
        {
            _activeTabIndex = tabIndex;
            OnTabChanged(new TabChangedEventArgs(GetActiveTab()));
        }
    }

    public void UpdateTabState(int tabIndex, int cursorPosition, (int start, int length) selection, Vector scrollOffset)
    {
        if (_tabs.TryGetValue(tabIndex, out var tab))
        {
            tab.CursorPosition = cursorPosition;
            tab.Selection = selection;
            tab.ScrollOffset = scrollOffset;
        }
    }

    public int GetCursorPosition()
    {
        if (_activeTabIndex != -1 && _tabs.TryGetValue(_activeTabIndex, out var activeTab))
            return activeTab.EditorViewModel.CursorPosition;
        return 0;
    }

    public (int start, int length) GetSelection()
    {
        if (_activeTabIndex != -1 && _tabs.TryGetValue(_activeTabIndex, out var activeTab))
            return activeTab.EditorViewModel.Selection;
        return (0, 0);
    }

    public Vector GetScrollOffset()
    {
        if (_activeTabIndex != -1 && _tabs.TryGetValue(_activeTabIndex, out var activeTab))
            return activeTab.EditorViewModel.ScrollOffset;
        return new Vector(0, 0);
    }
    
    public IEnumerable<TabInfo?> GetAllTabs()
    {
        return _tabs.Values;
    }

    public TabInfo? GetActiveTab()
    {
        return _activeTabIndex != -1 && _tabs.TryGetValue(_activeTabIndex, out var tab)
            ? tab
            : null;
    }

    protected virtual void OnTabChanged(TabChangedEventArgs e)
    {
        TabChanged?.Invoke(this, e);
    }
}