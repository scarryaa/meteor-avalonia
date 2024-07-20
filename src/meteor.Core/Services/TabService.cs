using meteor.Core.Interfaces.Services;
using meteor.Core.Models.Tabs;

namespace meteor.Core.Services;

public class TabService : ITabService
{
    private readonly Dictionary<int, TabInfo> _tabs = new();
    private int _activeTabIndex = -1;
    private int _nextTabIndex = 1;

    public ITextBufferService GetActiveTextBufferService()
    {
        if (_activeTabIndex == -1 || !_tabs.TryGetValue(_activeTabIndex, out var tab))
            return new TextBufferService();
        return tab.TextBufferService;
    }

    public void SwitchTab(int tabIndex)
    {
        if (_tabs.ContainsKey(tabIndex))
            _activeTabIndex = tabIndex;
        else
            throw new ArgumentException("Tab does not exist.");
    }

    public TabInfo AddTab(ITextBufferService textBufferService)
    {
        var tabIndex = _nextTabIndex++;
        var newTab = new TabInfo(tabIndex, $"Tab {tabIndex}", textBufferService);
        _tabs[tabIndex] = newTab;

        if (_activeTabIndex == -1)
            _activeTabIndex = tabIndex;

        return newTab;
    }

    public void CloseTab(int tabIndex)
    {
        if (_tabs.ContainsKey(tabIndex))
        {
            _tabs.Remove(tabIndex);

            if (_activeTabIndex == tabIndex)
                _activeTabIndex = _tabs.Keys.LastOrDefault(-1);
        }
    }

    public void CloseAllTabs()
    {
        _tabs.Clear();
        _activeTabIndex = -1;
        _nextTabIndex = 1;
    }

    public void CloseOtherTabs(int keepTabIndex)
    {
        if (_tabs.ContainsKey(keepTabIndex))
        {
            var tabToKeep = _tabs[keepTabIndex];
            _tabs.Clear();
            _tabs[keepTabIndex] = tabToKeep;
            _activeTabIndex = keepTabIndex;
        }
    }

    public IEnumerable<TabInfo> GetAllTabs()
    {
        return _tabs.Values;
    }

    public TabInfo? GetActiveTab()
    {
        return _activeTabIndex != -1 && _tabs.TryGetValue(_activeTabIndex, out var tab)
            ? tab
            : null;
    }
}